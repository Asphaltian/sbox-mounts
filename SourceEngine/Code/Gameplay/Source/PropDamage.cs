namespace Source1;

/// <summary>Applies the mounted model's damage modifiers before the prop loses health.</summary>
public sealed class PropDamage : Component
{
	[Property] public Doo OnBreak { get; set; }
	Prop _prop;
	PropFire _fire;

	internal static void Attach( Prop prop )
	{
		if ( prop.Model?.GetData<PropDamageModifiers>() is not null )
			prop.GetOrAddComponent<PropDamage>();
	}

	protected override void OnEnabled()
	{
		_prop = GetComponent<Prop>();
		if ( !_prop.IsValid() ) return;
		_prop.OnPropTakeDamage += OnDamage;
		if ( OnBreak is not null ) _prop.OnPropBreak += BreakOutput;
	}

	protected override void OnDisabled()
	{
		if ( !_prop.IsValid() ) return;
		_prop.OnPropTakeDamage -= OnDamage;
		_prop.OnPropBreak -= BreakOutput;
	}

	[Rpc.Host]
	void BreakOutput() => RunDoo( OnBreak, c => c.SetArgument( "user", _prop.LastAttacker ) );

	void OnDamage( DamageInfo hit )
	{
		var bullet = hit.Tags.Has( DamageTags.Bullet );
		var blast = hit.Tags.Has( DamageTags.Explosion );
		if ( !(bullet || blast) || _prop.Model?.GetData<PropDamageModifiers>() is not { } modifiers ) return;

		var projectile = blast && hit.Weapon.IsValid() ? hit.Weapon.GetComponent<Projectile>() : null;
		var scale = bullet ? modifiers.Bullet * (hit.Tags.Has( "buckshot" ) ? 2 : 1) : 1;
		if ( blast && projectile is null or { Blast: Projectile.BlastMode.PropFalloff } )
		{
			scale *= modifiers.Explosive;

			if ( projectile is { Radius: > 0, Blast: Projectile.BlastMode.PropFalloff } )
			{
				var bounds = _prop.Model.PhysicsBounds.Transform( WorldTransform );
				var testZ = projectile.UnderwaterBlast ? bounds.Mins.z : bounds.Maxs.z;
				if ( SourceWaterMap.IsUnderwater( Scene, bounds.Center.WithZ( testZ ) ) != projectile.UnderwaterBlast )
				{
					hit.Damage = 0;
					return;
				}

				// The blast trace already found the exposed surface of this prop.
				if ( GameObject == GameObject.Root )
				{
					var fraction = hit.Origin.Distance( hit.Position ) / projectile.Radius;
					hit.Damage = projectile.Damage * (1 - fraction.Clamp( 0, 1 ));
				}
			}
		}
		hit.Damage = System.MathF.Truncate( hit.Damage * scale );
		if ( blast && !_fire.IsValid() && hit.Damage > 0 && hit.Damage < _prop.Health && !modifiers.FireResistant
			&& projectile is { Blast: not Projectile.BlastMode.Direct } )
			_fire = PropFire.Ignite( _prop );
	}
}

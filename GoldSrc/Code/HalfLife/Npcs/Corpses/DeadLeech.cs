namespace GoldSrc.HalfLife;

[Hide]
public sealed class DeadLeech : NpcCorpse
{
	internal override string FinalSequence => "death_end";
	internal override bool WaitForFloor => true;
	internal override float GravityScale => WaterBody.Find( Scene, WorldPosition ).IsValid() ? 0.02f : 1;

	float _spin;

	protected override void OnStart()
	{
		base.OnStart();
		if ( Networking.IsHost && DeathSequence.Equals( "death", System.StringComparison.OrdinalIgnoreCase ) && Game.Random.Int( 0, 99 ) < 70 )
			_spin = Game.Random.Float( -720, 720 );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( Networking.IsHost && !Landed && _spin != 0 ) WorldRotation *= Rotation.FromYaw( _spin * Time.Delta );
	}
}

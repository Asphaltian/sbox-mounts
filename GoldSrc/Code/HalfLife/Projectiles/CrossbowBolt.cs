namespace GoldSrc.HalfLife;

[Hide]
public sealed class CrossbowBolt : NpcProjectile
{
	internal override string HitTag => "bullet";

	internal override void OnImpact( IDamageable victim, Vector3 position, Vector3 normal )
	{
		base.OnImpact( victim, position, normal );
		if ( Sandbox.Mounting.Directory.Get( "hl1" ) is HalfLifeMount mount )
			mount.NpcAudio.Emit( victim is null ? "weapons/xbow_hit1.wav" : "weapons/xbow_hitbod1.wav", position );
	}
}

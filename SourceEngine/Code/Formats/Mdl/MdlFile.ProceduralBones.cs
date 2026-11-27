using Sample = Sandbox.ModelBuilder.PoseConstraintSample;

namespace Source1;

internal sealed partial class MdlFile
{
	private readonly record struct QuaternionProcedure( int Control, int Count, int TriggerOffset );

	private readonly record struct QuaternionTrigger( float InverseRadius, Rotation Driver, Vector3 Position, Rotation Rotation );

	internal sealed record BoneCorrection( int Driver, int Target, Sample[] Samples );

	internal BoneCorrection[] QuaternionBones { get; }

	private BoneCorrection ReadQuaternionBone( Stream stream, long offset, int target )
	{
		stream.Position = offset;
		var (control, count, triggers) = LumpReader<QuaternionProcedure>.ReadSingleFromStream( stream );
		if ( (uint)control >= _bones.Length || count is < 1 or > 32 )
			throw new InvalidDataException( "Invalid quaternion interpolation procedure." );
		if ( _bones[control].Parent < 0 ) return null;

		stream.Position = offset + triggers;
		var entries = LumpReader<QuaternionTrigger>.ReadLumpFromStream( stream, count );
		var samples = System.Array.ConvertAll( entries, t => new Sample( t.Driver, t.Position, t.Rotation, (1.0f / t.InverseRadius).RadianToDegree() ) );
		return new( control, target, samples );
	}
}

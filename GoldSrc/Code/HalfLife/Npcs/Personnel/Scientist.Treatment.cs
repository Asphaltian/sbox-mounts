namespace GoldSrc.HalfLife;

public sealed partial class Scientist
{
	const int Arm = 33, Disarm = 34;
	StudioSequence _drawNeedle, _inject, _putAway;
	GameObject _patient;
	TimeUntil _nextHeal;

	void SetUpTreatment( ModelLoader loader )
	{
		_drawNeedle = loader.Sequences.FirstOrDefault( s => s.Description.Activity == Arm );
		_inject = loader.Sequences.FirstOrDefault( s => s.Description.Activity == Melee );
		_putAway = loader.Sequences.FirstOrDefault( s => s.Description.Activity == Disarm );
	}

	internal override bool AssistLeader()
	{
		if ( PhysicsCarrier.IsValid() || !NeedsTreatment( Leader ) || _drawNeedle is null || _inject is null || _putAway is null ) return false;
		var distance = WorldPosition.Distance( Leader.WorldPosition );
		if ( distance > 128 ) return false;
		if ( distance > 50 )
		{
			MoveToward( Leader.WorldPosition, 50 );
			return true;
		}

		_patient = Leader;
		Movement?.Stop();
		Say( "HEAL", _patient, 2 );
		PlayAction( _drawNeedle );
		return true;
	}

	bool NeedsTreatment( GameObject target ) => _nextHeal <= 0 && target.IsValid() && target == Leader
		&& target.GetComponent<Component.IDamageable>() is { Health: > 0, MaxHealth: > 0 } patient && patient.Health <= patient.MaxHealth * 0.5f;

	void UpdateTreatment()
	{
		if ( _patient is not null )
		{
			if ( Action != _drawNeedle.Name && Action != _inject.Name && Action != _putAway.Name )
				EndTreatment();
			else if ( Action != _putAway.Name && (!_patient.IsValid() || _patient != Leader ||
				(Action == _inject.Name && WorldPosition.DistanceSquared( _patient.WorldPosition ) > 90 * 90)) )
				PlayAction( _putAway );
			else if ( _patient.IsValid() )
				FacePatient();
		}
	}

	void FacePatient()
	{
		var direction = (_patient.WorldPosition - WorldPosition).WithZ( 0 );
		if ( direction.IsNearZeroLength ) return;
		var facing = Rotation.LookAt( direction, Vector3.Up );
		var angle = WorldRotation.Distance( facing );
		if ( angle > 0.1f )
			WorldRotation = Rotation.Slerp( WorldRotation, facing, System.MathF.Min( 1, 90 * Time.Delta / angle ) );
	}

	internal override void HandleActionEvent( int id )
	{
		if ( id == 2 ) EquipmentDrawn = true;
		if ( id == 3 ) EquipmentDrawn = false;
		if ( id != 1 || !NeedsTreatment( _patient ) || WorldPosition.DistanceSquared( _patient.WorldPosition ) > 100 * 100 ) return;
		var patient = _patient.GetComponent<Component.IDamageable>();
		patient.Health = System.MathF.Min( patient.MaxHealth, patient.Health + 25 );
		_nextHeal = 60;
	}

	internal override void ApplyEquipment() => Renderer.SetBodyGroup( "needle", EquipmentDrawn ? 1 : 0 );

	void FinishTreatmentAction( string name )
	{
		if ( _patient is null ) return;
		if ( name == _drawNeedle.Name ) PlayAction( NeedsTreatment( _patient ) ? _inject : _putAway );
		else if ( name == _inject.Name ) PlayAction( _putAway );
		else if ( name == _putAway.Name ) EndTreatment();
	}

	protected override void OnCarryStarted( GameObject carrier )
	{
		base.OnCarryStarted( carrier );
		if ( !Networking.IsHost || _patient is null ) return;
		InterruptAction();
		EndTreatment();
	}

	void EndTreatment()
	{
		EquipmentDrawn = false;
		_patient = null;
	}
}

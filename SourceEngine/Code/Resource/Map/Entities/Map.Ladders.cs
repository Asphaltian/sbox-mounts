using System;

namespace Source1;

partial class MapLoader
{
	private static void BuildBrushLadders( BspFile bsp, GameObject world, HashSet<int> brushIds )
	{
		var model = bsp.Models[0];
		var center = (model.Mins + model.Maxs) * 0.5f;
		var radius = (model.Maxs - model.Mins).Length + 1;
		var points = new List<Vector3>();
		var face = new List<Vector3>();
		var clipped = new List<Vector3>();
		foreach ( var id in brushIds )
		{
			var brush = bsp.collisionBrushes[id];
			if ( (brush.Contents & BspContents.Ladder) == 0 ) continue;
			var sides = bsp.collisionBrushSides.AsSpan( brush.FirstSide, brush.SideCount );
			points.Clear();
			float largestArea = 0;
			var rotation = Rotation.Identity;
			foreach ( var side in sides )
			{
				if ( side.Bevel != 0 ) continue;
				var normal = bsp.Planes[side.Plane].Normal;
				var horizontalFace = MathF.Abs( normal.z ) >= 0.9f;
				var across = Vector3.Cross( horizontalFace ? Vector3.Right : Vector3.Up, normal ).Normal;
				BuildBrushFace( bsp, sides, side, across, center, radius, ref face, ref clipped );
				if ( face.Count < 3 ) continue;
				points.AddRange( face );
				if ( horizontalFace ) continue;

				// Some maps paint every side with nodraw. Use the broad climbing face to orient the trigger.
				float area = 0;
				for ( int i = 2; i < face.Count; i++ )
					area += Vector3.Cross( face[i - 1] - face[0], face[i] - face[0] ).Length;
				if ( area <= largestArea ) continue;
				largestArea = area;
				rotation = Rotation.LookAt( normal, Vector3.Up );
			}

			if ( largestArea == 0 ) continue;
			var bounds = BBox.FromPoints( points.Select( p => rotation.Inverse * p ) );
			var position = rotation * bounds.Center;
			if ( bsp.IsSkyboxPosition( position ) ) continue;
			var ladder = CreateLadder( world, position, rotation );
			var hull = ladder.AddComponent<HullCollider>();
			hull.Type = HullCollider.PrimitiveType.Points;
			// Source traces the player hull two units toward the brush to attach.
			// Extend the trigger along the face normal to reach through the physics contact gap.
			foreach ( var point in points )
			{
				var local = rotation.Inverse * (point - position);
				hull.Points.Add( local + Vector3.Forward * 2 );
				hull.Points.Add( local - Vector3.Forward * 2 );
			}
			EnableLadder( hull );
		}
	}

	private sealed class EndpointLadderLoader : EntityLoader
	{
		public override bool SupportsSkybox => false;

		public override void Load( EntityBuilder builder, MapEntity entity )
		{
			if ( (entity.Integer( "spawnflags" ) & 1) != 0 || entity.Integer( "StartDisabled" ) != 0
				|| entity.Get( "point0" ) is null || entity.Get( "point1" ) is null ) return;

			var top = entity.Vector( "point0" );
			var bottom = entity.Vector( "point1" );
			var up = (top - bottom).Normal;
			if ( up.z == 0 ) return;
			if ( up.z < 0 ) up = -up;
			var forward = Rotation.From( entity.Angles ).Forward.SubtractDirection( up ).Normal;
			if ( forward.IsNearlyZero() ) forward = Vector3.Cross( up, Vector3.Right ).Normal;
			var ladder = CreateLadder( builder.World, (top + bottom) * 0.5f, Rotation.LookAt( forward, up ) );
			var box = ladder.AddComponent<BoxCollider>();
			box.Scale = new Vector3( 8, 32, top.Distance( bottom ) );
			EnableLadder( box );
		}
	}

	private static GameObject CreateLadder( GameObject world, Vector3 position, Rotation rotation )
	{
		var ladder = new GameObject( false, "ladder" ) { Parent = world, IsStatic = true, LocalPosition = position, LocalRotation = rotation };
		ladder.Tags.Add( "ladder" );
		return ladder;
	}

	private static void EnableLadder( Collider collider )
	{
		collider.Static = true;
		collider.IsTrigger = true;
		collider.GameObject.Enabled = true;
	}
}

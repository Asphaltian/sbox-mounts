using System;

namespace Source1;

partial class MapLoader
{
	private static HashSet<int> GetModelBrushes( BspFile bsp, int modelIndex )
	{
		if ( bsp.leafBrushes.Length == 0 ) return [];
		var pending = new Stack<int>();
		var model = bsp.Models[modelIndex];
		var visited = new HashSet<int>();
		var brushIds = new HashSet<int>();
		pending.Push( model.HeadNode );
		while ( pending.TryPop( out var index ) )
		{
			if ( !visited.Add( index ) ) continue;
			if ( index >= 0 )
			{
				var node = bsp.nodes[index];
				pending.Push( node.Front );
				pending.Push( node.Back );
				continue;
			}

			var leaf = bsp.leaves[~index];
			for ( int i = 0; i < leaf.BrushCount; i++ )
				brushIds.Add( bsp.leafBrushes[leaf.FirstBrush + i] );
		}
		return brushIds;
	}

	private static void BuildBrushFace( BspFile bsp, ReadOnlySpan<BspFile.CollisionBrushSide> sides, BspFile.CollisionBrushSide side,
		Vector3 tangent, Vector3 center, float radius, ref List<Vector3> polygon, ref List<Vector3> clipped )
	{
		var plane = bsp.Planes[side.Plane];
		var normal = plane.Normal;
		var across = Vector3.Cross( normal, tangent ) * radius;
		var along = tangent * radius;
		var origin = center + normal * (plane.Dist - Vector3.Dot( center, normal ));
		polygon.Clear();
		polygon.Add( origin - along - across );
		polygon.Add( origin + along - across );
		polygon.Add( origin + along + across );
		polygon.Add( origin - along + across );
		foreach ( var other in sides )
		{
			if ( other.Bevel != 0 || other.Plane == side.Plane ) continue;
			ClipBrushFace( polygon, bsp.Planes[other.Plane], clipped );
			(polygon, clipped) = (clipped, polygon);
			if ( polygon.Count < 3 ) break;
		}
	}

	private static void ClipBrushFace( List<Vector3> polygon, BspPlane plane, List<Vector3> output )
	{
		output.Clear();
		var previous = polygon[^1];
		float from = Vector3.Dot( previous, plane.Normal ) - plane.Dist;
		foreach ( var point in polygon )
		{
			float to = Vector3.Dot( point, plane.Normal ) - plane.Dist;
			if ( (from > 0) != (to > 0) )
				output.Add( previous + (point - previous) * (from / (from - to)) );
			if ( to <= 0 ) output.Add( point );
			previous = point;
			from = to;
		}
	}
}

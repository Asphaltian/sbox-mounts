using System;

namespace Source1;

partial class MapLoader
{
	// VBSP strips nodraw backing from the render mesh, including the solid roof above inset lights.
	private static void AddHiddenShadowFaces( BspFile bsp, int modelIndex, Func<Vector3, MeshGroup> chunkFor, Func<int, bool> isOpaque )
	{
		if ( bsp.leafBrushes.Length == 0 ) return;
		var backingTextures = new Dictionary<int, bool>();
		var model = bsp.Models[modelIndex];
		var pending = new Stack<int>();
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

		var polygon = new List<Vector3>();
		var clipped = new List<Vector3>();
		var center = (model.Mins + model.Maxs) * 0.5f;
		var radius = (model.Maxs - model.Mins).Length + 1;
		foreach ( var id in brushIds )
		{
			var brush = bsp.collisionBrushes[id];
			if ( (brush.Contents & (BspContents.Solid | BspContents.Opaque)) == 0 ) continue;
			var sides = bsp.collisionBrushSides.AsSpan( brush.FirstSide, brush.SideCount );
			bool hasBacking = false, passesLight = false;
			foreach ( var side in sides )
			{
				if ( side.Bevel != 0 || side.TexInfo < 0 ) continue;
				var info = bsp.TexInfos[side.TexInfo];
				if ( side.DispInfo > 0 || (info.Flags & (BspSurfaceFlags.SkyMask | BspSurfaceFlags.Warp | BspSurfaceFlags.NoShadows)) != 0
					|| ((info.Flags & BspSurfaceFlags.NoDraw) == 0 && info.TexData >= 0 && !isOpaque( info.TexData )) )
				{
					passesLight = true;
					break;
				}
				hasBacking |= IsBacking( side.TexInfo );
			}
			if ( !hasBacking || passesLight ) continue;

			foreach ( var side in sides )
			{
				if ( side.Bevel != 0 || !IsBacking( side.TexInfo ) ) continue;
				var plane = bsp.Planes[side.Plane];
				var normal = plane.Normal;
				var tangent = Vector3.Cross( normal, MathF.Abs( normal.z ) < 0.9f ? Vector3.Up : Vector3.Right ).Normal;
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
					ClipShadowFace( polygon, bsp.Planes[other.Plane], clipped );
					(polygon, clipped) = (clipped, polygon);
					if ( polygon.Count < 3 ) break;
				}
				if ( polygon.Count < 3 ) continue;
				if ( modelIndex == 0 && bsp.skyboxAreas.Count > 0 )
				{
					var bounds = new BBox( polygon[0], polygon[0] );
					foreach ( var point in polygon ) bounds = bounds.AddPoint( point );
					if ( bsp.IsSkyboxBounds( new BBox( bounds.Mins - Vector3.One, bounds.Maxs + Vector3.One ), pending, out _ ) ) continue;
				}
				var group = chunkFor( polygon[0] );
				int first = group.Vertices.Count;
				foreach ( var point in polygon )
					group.Vertices.Add( new BlendLightmapVertex( point, normal, tangent, default, default ) );
				for ( int i = 1; i < polygon.Count - 1; i++ )
				{
					// Hidden surfaces have no visible mesh to cast from the opposite side.
					group.Indices.AddRange( [first, first + i, first + i + 1, first, first + i + 1, first + i] );
				}
			}
		}

		bool IsBacking( int texInfo )
		{
			if ( texInfo < 0 ) return false;
			var info = bsp.TexInfos[texInfo];
			if ( info.TexData < 0 || (info.Flags & BspSurfaceFlags.NoDraw) == 0 ) return false;
			if ( backingTextures.TryGetValue( info.TexData, out var backing ) ) return backing;
			var name = bsp.GetTexDataString( info.TexData );
			return backingTextures[info.TexData] = string.Equals( name, "tools/toolsnodraw", StringComparison.OrdinalIgnoreCase )
				|| string.Equals( name, "tools/toolsblocklight", StringComparison.OrdinalIgnoreCase );
		}
	}

	private static void ClipShadowFace( List<Vector3> polygon, BspPlane plane, List<Vector3> output )
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

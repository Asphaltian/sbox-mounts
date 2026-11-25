using System;

namespace Source1;

/// <summary>
/// Packs leaves with matching XY dimensions into three volume textures. All three share
/// one normalized Z range per leaf, with six ambient cube faces per direct-light slice.
/// </summary>
internal sealed class BspProbeTextures( string resourcePrefix ) : IDisposable
{
	private const int MaxDepth = 2048;
	private readonly record struct Leaf( LightProbeVolume Volume, int Depth, byte[] Data );

	private sealed class Page
	{
		public readonly List<Leaf> Leaves = [];
		public int Depth;
	}

	private readonly Dictionary<(int Width, int Height), Page> pages = [];
	private readonly HashSet<Texture> textures = [];

	public int Count => textures.Count;

	public void Add( LightProbeVolume volume, Vector3Int counts, BspProbeTextureData data )
	{
		var size = (counts.x, counts.y);
		if ( !pages.TryGetValue( size, out var page ) )
			pages.Add( size, page = new Page() );

		if ( (page.Depth + counts.z) * 6 > MaxDepth )
			Upload( counts.x, counts.y, page );

		page.Leaves.Add( new Leaf( volume, counts.z, data.Data ) );
		page.Depth += counts.z;
	}

	public void Finish()
	{
		foreach ( var ((width, height), page) in pages )
			Upload( width, height, page );

		pages.Clear();
	}

	private void Upload( int width, int height, Page page )
	{
		int probes = width * height * page.Depth;
		var ambient = new byte[probes * 48];
		var indices = new byte[probes * 4];
		var scalars = new byte[probes * 8];
		int offset = 0;
		foreach ( var leaf in page.Leaves )
		{
			int count = width * height * leaf.Depth;
			leaf.Data.AsSpan( 0, count * 48 ).CopyTo( ambient.AsSpan( offset * 48 ) );
			leaf.Data.AsSpan( count * 48, count * 4 ).CopyTo( indices.AsSpan( offset * 4 ) );
			leaf.Data.AsSpan( count * 52 ).CopyTo( scalars.AsSpan( offset * 8 ) );
			offset += count;
		}

		var light = CreateTexture( width, height, page.Depth * 6, ImageFormat.RGBA16161616F, ambient );
		var lightIndices = CreateTexture( width, height, page.Depth, ImageFormat.RGBA8888, indices );
		var lightScalars = CreateTexture( width, height, page.Depth, ImageFormat.RGBA16161616F, scalars );
		int slice = 0;
		foreach ( var leaf in page.Leaves )
		{
			leaf.Volume.Texture = light;
			leaf.Volume.IndicesTexture = lightIndices;
			leaf.Volume.ScalarsTexture = lightScalars;
			leaf.Volume.AtlasZOffset = (float)slice / page.Depth;
			leaf.Volume.AtlasZScale = (float)leaf.Depth / page.Depth;
			slice += leaf.Depth;
		}

		page.Leaves.Clear();
		page.Depth = 0;
	}

	private Texture CreateTexture( int width, int height, int depth, ImageFormat format, byte[] data )
	{
		var texture = Texture.CreateVolume( width, height, depth, format )
			.WithName( $"{resourcePrefix}/probes/atlas_{textures.Count}" ).WithStaticUsage().WithData( data ).Finish();
		if ( texture is null || !texture.IsValid || texture.IsError )
			throw new InvalidOperationException( $"{resourcePrefix}: could not create {width}x{height}x{depth} {format} probe atlas." );

		textures.Add( texture );
		return texture;
	}

	public bool Contains( Texture texture ) => textures.Contains( texture );

	public void Dispose()
	{
		foreach ( var texture in textures )
			texture.Dispose();

		textures.Clear();
		pages.Clear();
	}
}

namespace GoldSrc;

public partial class HalfLifeMount
{
	readonly Dictionary<string, Sprite> _indexedSprites = new();

	readonly Dictionary<string, Sprite[]> _indexedSpriteFrames = new();

	readonly Dictionary<string, Material> _trailMaterials = new();

	internal Material GetTrailMaterial( string name )
	{
		if ( _trailMaterials.TryGetValue( name, out var material ) ) return material;
		if ( GetIndexedSprite( name ) is not { } sprite ) return null;
		material = Material.Load( "materials/default/default_line.vmat" ).CreateCopy();
		material.Set( "g_tColor", sprite.Animations[0].Frames[0].Texture );
		return _trailMaterials[name] = material;
	}

	internal Sprite[] GetIndexedSpriteFrames( string name )
	{
		if ( _indexedSpriteFrames.TryGetValue( name, out var frames ) ) return frames;
		if ( GetIndexedSprite( name ) is not { } sprite ) return null;
		return _indexedSpriteFrames[name] = sprite.Animations[0].Frames.Select( frame => Sprite.FromTexture( frame.Texture ) ).ToArray();
	}

	internal Sprite GetIndexedSprite( string name )
	{
		if ( _indexedSprites.TryGetValue( name, out var sprite ) )
			return sprite;

		var path = GameDirs.Select( dir => $"{dir}/sprites/{name}.spr" ).FirstOrDefault( FileSystem.FileExists );
		if ( path is null ) return null;
		using var reader = new BinaryReader( FileSystem.OpenRead( path ) );
		if ( reader.BaseStream.Length < 42 || reader.ReadInt32() != 0x50534449 || reader.ReadInt32() != 2 ) return null;
		reader.ReadInt32(); // billboard orientation
		var format = reader.ReadInt32();
		if ( format is < 0 or > 3 ) return null; // normal, additive, index-alpha, alpha-test
		reader.BaseStream.Position = 28;
		var count = reader.ReadInt32();
		reader.BaseStream.Position = 40;
		if ( count is < 1 or > 256 || reader.ReadUInt16() != 256 ) return null;
		var palette = reader.ReadBytes( 768 );
		if ( palette.Length != 768 ) return null;
		var frames = new List<Texture>();
		for ( var i = 0; i < count; ++i )
		{
			if ( reader.BaseStream.Length - reader.BaseStream.Position < 20 || reader.ReadInt32() != 0 ) return null;
			reader.BaseStream.Position += 8; // frame origin
			var width = reader.ReadInt32();
			var height = reader.ReadInt32();
			if ( width is < 1 or > 1024 || height is < 1 or > 1024 ) return null;
			var indices = reader.ReadBytes( width * height );
			if ( indices.Length != width * height ) return null;
			var pixels = new byte[indices.Length * 4];
			for ( var j = 0; j < indices.Length; ++j )
			{
				var color = format == 2 ? 765 : indices[j] * 3;
				pixels[j * 4] = palette[color];
				pixels[j * 4 + 1] = palette[color + 1];
				pixels[j * 4 + 2] = palette[color + 2];
				pixels[j * 4 + 3] = format == 2 ? indices[j] : format == 3 && indices[j] == 255 ? (byte)0 : (byte)255;
			}
			frames.Add( Texture.Create( width, height ).WithData( pixels ).WithMips().Finish() );
		}
		return _indexedSprites[name] = Sprite.FromTextures( frames, 10 );
	}
}

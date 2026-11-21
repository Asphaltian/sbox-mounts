using System;

namespace GoldSrc.HalfLife;

[Library]
internal sealed partial class NpcAudio
{
	readonly Dictionary<string, string> _files = new( StringComparer.OrdinalIgnoreCase );
	readonly Dictionary<string, List<string>> _variants = new( StringComparer.OrdinalIgnoreCase );

	internal NpcAudio( HalfLifeMount mount )
	{
		var sounds = mount.GetAll( ResourceType.Sound ).OfType<WavSoundLoader>().ToArray();
		foreach ( var dir in mount.GameDirs )
		{
			var prefix = $"{dir}/sound/";
			foreach ( var sound in sounds )
			{
				if ( !sound.RelativePath.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) )
					continue;

				var file = sound.RelativePath[prefix.Length..^".vsnd".Length];
				if ( !_files.TryAdd( file, sound.Path ) )
					continue;

				var stem = file.AsSpan( 0, file.Length - ".wav".Length );
				while ( !stem.IsEmpty && char.IsAsciiDigit( stem[^1] ) )
					stem = stem[..^1];
				var key = stem.ToString();
				if ( !_variants.TryGetValue( key, out var variants ) )
					_variants.Add( key, variants = [] );
				variants.Add( sound.Path );
			}
		}
		var sentences = mount.GameDirs.Select( dir => $"{dir}/sound/sentences.txt" ).FirstOrDefault( mount.FileSystem.FileExists );
		if ( sentences is not null ) IndexSpeechGroups( mount.FileSystem.ReadAllText( sentences ) );
	}

	internal string Find( string sample )
	{
		if ( string.IsNullOrWhiteSpace( sample ) )
			return null;
		if ( sample.EndsWith( ".wav", StringComparison.OrdinalIgnoreCase ) )
			return _files.GetValueOrDefault( sample.TrimStart( '*' ).Replace( '\\', '/' ) );
		if ( !_variants.TryGetValue( sample, out var sounds ) )
			return null;
		return sounds[Game.Random.Int( 0, sounds.Count - 1 )];
	}

	internal bool Emit( string sample, Vector3 position, float volume = 1, float pitch = 1, VoicedNpc speaker = null )
	{
		if ( sample?.StartsWith( '!' ) == true ) return EmitSentence( sample[1..], position, volume, pitch, speaker );
		var path = Find( sample );
		if ( path is null )
			return false;
		Broadcast( path, position, volume, pitch, speaker );
		return true;
	}

	[Rpc.Broadcast( NetFlags.HostOnly )]
	static void Broadcast( string path, Vector3 position, float volume, float pitch, VoicedNpc speaker )
	{
		if ( speaker.IsValid() ) speaker.BeginVoice();
		var handle = Play( path, position, volume, pitch );
		if ( speaker.IsValid() ) speaker.TrackVoice( handle );
	}

	internal static SoundHandle Play( string path, Vector3 position, float volume = 1, float pitch = 1 )
	{
		if ( path is not null && SoundFile.Load( path ) is { } sound )
		{
			var handle = Sound.PlayFile( sound );
			if ( handle.IsValid() )
			{
				handle.Position = position;
				handle.Distance = 1000;
				handle.Volume = volume;
				handle.Pitch = pitch;
				return handle;
			}
		}
		return null;
	}
}

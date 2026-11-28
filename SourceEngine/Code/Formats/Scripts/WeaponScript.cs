using System;
using System.Text;

namespace Source1;

/// <summary>Reads a Source <c>WeaponData</c> script. Sound roles map to script sound names, not mounted resources.</summary>
internal static class WeaponScript
{
	internal static string Decode( byte[] data, string path, string iceKey )
	{
		if ( path.EndsWith( ".ctx", StringComparison.OrdinalIgnoreCase ) )
		{
			if ( iceKey is null )
				throw new NotSupportedException( $"{path}: encrypted weapon script has no known key." );
			new IceKey( Encoding.ASCII.GetBytes( iceKey ) ).Decrypt( data );
		}
		return Encoding.UTF8.GetString( data ).TrimEnd( '\0' );
	}

	internal static SourceModelWeaponData Read( string text, string path, SkillConfig skill ) => Read( Parse( text, path ), path, skill );

	/// <summary>The script's <c>WeaponData</c> block, or null.</summary>
	internal static VmtNode Parse( string text, string path ) =>
		// HUD font glyphs such as "\" are literal: Source reads weapon scripts without escape sequences.
		VmtFile.ParseKeyValues( text, path, diagnostic => Log.Trace( $"{diagnostic.Path}:{diagnostic.Line}: {diagnostic.Message}" ), escapeSequences: false )
			.FirstOrDefault( x => x.IsBlock && x.Name.Equals( "WeaponData", StringComparison.OrdinalIgnoreCase ) );

	internal static SourceModelWeaponData Read( VmtNode root, string path, SkillConfig skill, SourceGameMount mount = null )
	{
		if ( root is null )
			return null;

		var p = new VmtParameters( root.Values, path );
		var weaponClass = Path.GetFileNameWithoutExtension( path );
		var primaryAmmo = AmmoName( p.String( "primary_ammo" ) );
		var secondaryAmmo = AmmoName( p.String( "secondary_ammo" ) );
		var damage = p.Contains( "Damage" ) ? p.Float( "Damage" ) : (float?)null;
		var clip = p.Integer( "clip_size", -1 );
		var clip2 = p.Integer( "clip2_size", -1 );
		var data = mount?.CreateWeaponData( p ) ?? new SourceModelWeaponData();
		data.ClassName = weaponClass;
		data.ViewModel = ModelPath( p, "viewmodel" );
		data.WorldModel = ModelPath( p, "playermodel" );
		data.DroppedModel = ModelPath( p, "DroppedModel" );
		data.SilencerModel = ModelPath( p, "SilencerModel" );
		data.PrimaryAmmo = primaryAmmo;
		data.SecondaryAmmo = secondaryAmmo;
		data.PrintName = p.String( "printname" );
		data.ScriptDamage = damage;
		data.PlayerDamage = damage ?? skill.WeaponDamage( primaryAmmo, weaponClass );
		data.NpcDamage = damage ?? skill.WeaponDamage( primaryAmmo, weaponClass, npc: true );
		data.SecondaryPlayerDamage = skill.WeaponDamage( secondaryAmmo, null );
		data.SecondaryNpcDamage = skill.WeaponDamage( secondaryAmmo, null, npc: true );
		data.BlastRadius = skill.WeaponRadius( primaryAmmo, weaponClass );
		data.SecondaryBlastRadius = skill.WeaponRadius( secondaryAmmo, null );
		data.ClipSize = clip;
		data.DefaultClip = p.Integer( "default_clip", clip > 0 ? clip * Math.Max( 1, p.Integer( "DefaultAmmoClips", 1 ) ) : clip );
		data.SecondaryClipSize = clip2;
		data.SecondaryDefaultClip = p.Integer( "default_clip2", clip2 );
		data.Bucket = p.Integer( "bucket", 0 );
		data.BucketPosition = p.Integer( "bucket_position", -1 ) is var position && position >= 0 ? position : null;
		data.Weight = p.Integer( "weight", -1 ) is var weight && weight >= 0 ? weight : null;
		data.CycleTime = p.Contains( "CycleTime" ) ? p.Float( "CycleTime" ) : p.Contains( "FireDelay" ) ? p.Float( "FireDelay" ) : null;
		data.ViewModelFov = p.Contains( "ViewModelFOV" ) ? p.Float( "ViewModelFOV" ) : null;
		data.ViewModelOffset = p.Vector( "vm_normal_offset", Vector3.Zero );
		data.IronSightOffset = p.Contains( "vm_ironsight_offset" ) ? p.Vector( "vm_ironsight_offset", Vector3.Zero ) : null;
		data.SecondaryAccuracy = p.Contains( "SecondaryAccuracy" ) ? p.Float( "SecondaryAccuracy" ) : null;
		data.SecondaryCycleTime = p.Contains( "SecondaryFireDelay" ) ? p.Float( "SecondaryFireDelay" ) : null;
		data.AccuracyMovePenalty = p.Contains( "AccuracyMovePenalty" ) ? p.Float( "AccuracyMovePenalty" ) : null;
		data.Accuracy = p.Contains( "Accuracy" ) ? p.Float( "Accuracy" ) : null;
		data.Recoil = p.Contains( "Recoil" ) ? p.Float( "Recoil" ) : null;
		data.Bullets = p.Contains( "Bullets" ) ? p.Integer( "Bullets" ) : null;
		data.FullAuto = p.Contains( "FullAuto" ) ? p.Boolean( "FullAuto" ) : null;
		data.Range = p.Contains( "Range" ) ? p.Float( "Range" ) : null;
		data.RangeModifier = p.Contains( "RangeModifier" ) ? p.Float( "RangeModifier" ) : null;
		data.WeaponType = p.String( "WeaponType" );
		data.Team = p.String( "Team" );
		data.PlayerAnimationExtension = p.String( "PlayerAnimationExtension" );
		data.BuiltRightHanded = p.Contains( "BuiltRightHanded" ) ? p.Boolean( "BuiltRightHanded" ) : null;
		data.MuzzleFlashStyle = p.String( "MuzzleFlashStyle" );
		data.MuzzleFlashScale = p.Contains( "MuzzleFlashScale" ) ? p.Float( "MuzzleFlashScale" ) : null;
		foreach ( var sound in root.Children.Where( x => x.IsBlock && x.Name.Equals( "SoundData", StringComparison.OrdinalIgnoreCase ) ).SelectMany( x => x.Children ) )
		{
			if ( !sound.IsBlock && !string.IsNullOrWhiteSpace( sound.Value ) )
				data.Sounds.TryAdd( sound.Name.ToLowerInvariant(), sound.Value );
		}
		return data;
	}

	private static string AmmoName( string name ) => name is "" or "None" ? null : name;

	private static string ModelPath( VmtParameters p, string key ) =>
		p.String( key ) is { } value && !string.IsNullOrWhiteSpace( value ) ? ScriptManifest.ContentPath( value ) : null;
}

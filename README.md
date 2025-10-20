<div>
  <a href='https://ko-fi.com/cityrobo' target='_blank'>
    <img height='60' style='border:0px;height:60px;' src='https://cdn.prod.website-files.com/5c14e387dab576fe667689cf/670f5a02fad2b4c413af6d15_support_me_on_kofi_badge_beige.png' alt='Ko-fi'/>
    <strong>cityrobo</strong>
  </a>
</div>

<div>
  <a href='https://throne.com/chomilk' target='_blank'>
    <img height='60' style='border:0px;height:60px;' src='https://thronecdn.com/common/integrations/panels/wishlist_button_small_rainbow.png?version=2' alt='Throne Wishlist'/>
    <strong>chomilk</strong>
  </a>
</div>

![Pistol1-1](https://github.com/user-attachments/assets/05356a6a-b8e5-4eac-aeb2-26e3ac8dac3a)

# H3VR PlayerBody System

A complete restructure and rewrite of the existing player body system by JerryAR and AngryNoob. Easier implementation and (hopefully!) better performance due to the slimmed down hierarchy.

**This is a fork of [cityrobo's H3VR PlayerBody System](https://github.com/cityrobo/H3VR-PlayerBody-System)** with additional updates and maintenance.

**Check the [wiki](https://github.com/chomiruku/H3VR-PlayerBody-System/wiki) on how to make your own PlayerBodies**

## Building from Source

### Prerequisites
- Visual Studio 2017+ or JetBrains Rider
- .NET Framework 3.5
- NuGet package manager

### Required Dependencies

The following DLLs must be placed in the `dep/` folder before building:

1. **H3MP.dll** - H3VR Multiplayer mod
   - Download from: https://github.com/TommySoupy/H3MP/releases

2. **OpenScripts2.dll** - OpenScripts2 library
   - Download from: https://github.com/cityrobo/OpenScripts2/blob/master/OpenScripts2/OpenScripts2.dll

3. **UnityEditor.dll** - Unity 5.6.7f1 Editor managed assembly
   - Located in: `Unity 5.6.7f1\Editor\Data\Managed\UnityEditor.dll`
   - Download Unity 5.6.7f1 from Unity's archive if needed

### Build Steps

1. Clone this repository
2. Download and place the required dependencies in the `dep/` folder
3. Restore NuGet packages
4. Build the solution in Release configuration
5. The compiled DLL will be output to the project root as `H3VRPlayerBodySystem.dll`

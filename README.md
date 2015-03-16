# SignForge

Sign all PBO files in one folder with a newly generated key file.

# Usage

Unpack the files somewhere in a an empty folder.

```
SignForge.exe [help] -s "source pbo files" [-a "authority name"] [-d "destination folder"] [-v] [-r]
```

Arguments:

`help` - (OPTIONAL) display the help information

`-r` - (OPTIONAL) remove all old .bisign files from source folder

`-v` - (OPTIONAL) verify all files using DSCheckSignatures.

`-s *source*` - folder where the pbos that need to be signed reside.

`-a *name*` - (OPTIONAL) authority name. If argument not provided then prompt.

`-d *destination*`- (OPTIONAL) folder to put private and public keys into. If not provided will be same as source directory.

# Usage on Linux with Wine
If you have a linux machine you can sign files as well! The tool is compatible to run under Wine.

Download the wine specific release or build in `Wine` configuration. Unpack the files somewhere on the linux machine like `/var/signforge/`.

```
wine SignForge.exe [help] -s "source pbo files" [-a "authority name"] [-d "destination folder"] [-v] [-r]
```

The arguments are the same as for windows (make sure you specify *nix filepaths though.
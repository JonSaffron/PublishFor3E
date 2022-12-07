Publish For 3E
--------------

This utility aims to make publishing in 3E a little easier by automating the stopping and starting of the application pools that sometimes keep the application's DLLs locked. The account running the utliity will need the appropriate priviledges to connect to and control the WAPI servers. 

You can specify the WAPIs manually on the command line, or the utility can try and determine them for itself by checking the NxNtfServer table. This will require that the person running the utility has an account in the 3E environment being targetted.

The code is compatible with .net framwork and .net 6.

The executable for .net framework can be built and will run standalone (without any other accompanying files) as long as the appropriate version of the framework is installed.
For .net 6, the visual studio packager can be run to produce a similar standalone executable, however it will be much larger in size.

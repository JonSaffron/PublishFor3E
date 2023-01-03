Publish For 3E
==============

This utility aims to make publishing changes in 3E a little easier by automating the stopping and starting of the application pools that sometimes keep the application's DLLs locked. The account running the utility will need the appropriate privileges to connect to and control the WAPI servers. 

You can specify the WAPIs manually on the command line, or the utility can try and determine them for itself by checking the NxNtfServer table. This will require that the person running the utility has an account in the 3E environment being targeted.

The code is compatible with .net framework 4.8 and .net 6.

The executable for .net framework can be built and will run standalone (without any other accompanying files) as long as the appropriate version of the framework is installed.
For .net 6, the visual studio packager can be run to produce a similar standalone executable, however it will be very much larger in size.

Example usage
-------------

For automatic discovery of WAPIs just specify a URL that identifies the environment:
~~~
> publish http://rdwap1/TE_3E_DEV/
~~~

If you need to explicitly specify a list of WAPIs use:
~~~
> publish http://rdwap1/TE_3E_DEV/ wapi1 wapi2 wapi3
~~~

After using one of the above commands, information about the environment that was published to will be saved to a PublishSettings.xml file so that future usage can be shortened to just the environment name, or a portion of it such as:
~~~
> publish dev
~~~

![](PublishFor3E/Resources/Screenshot.png?raw=true)

Publish For 3E
==============

This utility aims to make publishing in 3E a little easier by automating the stopping and starting of the application pools that sometimes keep the application's DLLs locked. The account running the utliity will need the appropriate priviledges to connect to and control the WAPI servers. 

You can specify the WAPIs manually on the command line, or the utility can try and determine them for itself by checking the NxNtfServer table. This will require that the person running the utility has an account in the 3E environment being targetted.

The code is compatible with .net framework 4.8 and .net 6.

The executable for .net framework can be built and will run standalone (without any other accompanying files) as long as the appropriate version of the framework is installed.
For .net 6, the visual studio packager can be run to produce a similar standalone executable, however it will be very much larger in size.

Exaple usage
------------

For automatic discovery of WAPIs just specify a URL that identifies the environment:
~~~
> publish http://rdwap1/TE_3E_DEV/
~~~

If you need to explictly specify a list of WAPIS use:
~~~
> publish http://rdwap1/TE_3E_DEV/ wapi1 wapi2 wapi3
~~~

The settings you enter above will be saved in a PublishSettings.xml file so that future invocation can be simplified:
~~~
> publish dev
~~~



![](PublishFor3E/Resources/Screenshot.png?raw=true)

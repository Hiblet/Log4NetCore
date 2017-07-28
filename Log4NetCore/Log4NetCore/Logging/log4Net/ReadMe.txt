Ensure that you right-click the log4Net.config file and set 'Copy To Output' to 'Always'.

This will ensure that the config file is put in the correct output directory.  
You can do this step manually if you wish.
Failing to do this results in the code not finding the config file and throwing.

Tip: If deploying to IIS, and the application is not starting, it may be throwing an exception.
To debug, call dotnet from a command prompt (as Admin) and try to run your compiled executable
or dll directly on the command line.  Any exceptions should be returned to the prompt.

Example Error (from Event Viewer)

Application 'MACHINE/WEBROOT/APPHOST/IDENTITYEXP1' with physical root 'C:\Websites\IdentityExp1\' 
failed to start process with commandline '"dotnet" .\IdentityExp1.dll', ErrorCode = '0x80004005
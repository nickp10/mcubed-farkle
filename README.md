# mcubed-farkle

Description
----
A simple desktop simulating the game of Farkle.

Creating New Projects
----
1. Right click the project and choose Properties.
1. Go to the Signing tab.
1. Check "Sign the ClickOnce manifests".
1. Click "Create Test Certificate...".
1. Enter "mCubed3Key" as the password and confirm password.
1. Click OK.
1. Rename the generated .pfx file by removing "_TemporaryKey" from the file name.
1. Check "Sign the assembly".
1. Click the "Choose a strong name key file" drop-down.
1. Select the generated .pfx file from above.
1. Click "Select from File...".
1. Browse to the generated .pfx file.
1. Enter the password from above.
1. Click OK.
1. Uncheck "Sign the ClickOnce manifests".
1. Right click the project and choose "Unload Project".
1. Click Yes to save the changes.
1. Right click the project and choose "Edit <PROJECT-NAME>.cspoj".
1. At the bottom of the file, add the following target:
    ```
      <Target Name="BeforePublish">
        <Exec Command="&quot;$(MSBuildProgramFiles32)\Microsoft SDKs\Windows\v7.0A\Bin\signtool.exe&quot; sign /f &quot;$(ProjectDir)$(AssemblyName).pfx&quot; /p mCubed3Key /v &quot;$(ProjectDir)obj\x86\$(ConfigurationName)\$(TargetFileName)&quot;" />
      </Target>
    ```
1. Right click the project and choose "Reload Project".
1. Click Yes to close the file in the editor and Yes to save the changes.

Creating Releases
----
1. Run all unit tests through Visual Studio to make sure they work.
1. Commit all changes.
1. Set the build mode to Release.
1. Build the solution.
1. Zip the bin/Release directory.
1. Upload the zip file to GitHub.


---------------------------------------------------------
DESCRIPTION:

SnippetExecutor is a Notepad++ plugin for compiling and executing snippets of code, and providing the output.  
This facilitates rapid development of code by allowing NP++ users to prototype and test small, simple sections
of code directly in notepad++.

Note: some features described in this document may not yet work.  Still under development.

---------------------------------------------------------

Use:
Build the project and add the .dll to your plugins folder.  Select the code to execute and select 
Plugins -> SnippetExecutor -> Run.  If nothing is selected this will run the entire current file up until the
output of the previous run, if it has been directed to append to the file.

Lines of code that are not contained in a class or function will be executed in the main function, which is
the entry point.  Data written to standard output will appear in the output location in np++, whether that is
the console window or a file depending on the user settings.  Text entered in the console or file will be directed
to standard input.

SnippetExecutor options can be specified in the options menu or overridden for one specific run by writing option
lines in the code to be executed.  Option lines begin with '>>' and specify a valid option.  invalid options will
be ignored with a warning message.  Option lines MUST be before any code, option lines that appear after the first
code line (defined as non-whitespace line that doesn't begin with '>>') will cause an error.

Example snippet:

>>lang java
>>run myRunArgument1 myRunArgument2

for(int i = 0; i < args.length; i++){
	myWrite(args[i]);
}

void myWrite(string s){
	System.out.println(s);
}

The above snippet would output the following:
--- SnippetExecutor 1/1/2011 ---
myRunArgument1
myRunArgument2
--- Finished ---

the '>>lang java' option explicitly specifies the language as java.  If not specified this is inferred from the file's language setting.
the '>>run ' option specifies runtime command line arguments.

the for loop is placed in the 'public static void main(string[] args)' method, so the snippet has access to the args[] parameter.
the myWrite function is placed in the same class outside the main method.
any classes would be placed in the same file outside the main class.

-----------------------------------------------------------------------------------------------------------------

Building the project:
The project should immediately be buildable.  One quirk of the project is that while it is a C# project, it requires C/C++'s lib.exe
tool.  This should come with microsoft's free Visual Studio C++ express, so make sure you get both Microsoft Visual Studio C# and C++.

After building simply find the .dll and copy it into notepad++'s plugins directory.


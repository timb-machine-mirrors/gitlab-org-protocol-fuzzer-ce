#Peach Enterprise Web

This is the project for the Peach Enterprise Web application.

Ignore Peach.Enterprise.Web for now, it is an older version.

The important thing to note is that this application's client side code is writen in TypeScript.

TypeScript files (*.ts) are checked in under app\ts.

When the project is built, the TypeScript files are translated to JavaScript and placed in app\js, though are not checked in.

Any web pages checked into this project should not refer to any CDN sources for JavaScript files, they should be included in the project using NuGet if possible.


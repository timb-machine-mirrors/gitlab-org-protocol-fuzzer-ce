<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PeachDownloader.Default" %>
<%
    // If user has already validated license, send them to downloads
    if ((bool)Session["LicenseValidation"])
    {
        Response.Redirect("Downloads.aspx", true);
    }
%>
<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1">
    <title>Peach Fuzzer Downloads</title>
</head>
<body>
    <form id="form1" runat="server">
        <!-- fnord -->
        <center>
			<table width="80%">
				<tbody><tr><td align="center">
					<img src="Images/peach_fuzzer.png" alt="Peach Fuzzer Professional" width="80%">
					<table width="80%">
						<tbody><tr><td align="center">
						    <p>
                                Welcome to the Peach Fuzzer download site!
                            </p>
                            <p>
                                After authenticating using your operations portal credentials, you will be able to access the Peach Fuzzer downloads.
                            </p>
                            <hr />

							<%
							if (Request["Error"] != null)
							{ 
								%>
								<p style="background-color:#FFC2C2">
									Incorrect username or password.  Please check and try again. If login issues persist please contact <strong>support@peachfuzzer.com</strong> for assistance.
	                            </p>
	                            <hr />
							<% 
							} 
							%>

                            <p>
                                Username:
								<asp:TextBox ID="TextBoxUser" runat="server"></asp:TextBox>
                                <br />Password:
								<asp:TextBox ID="TextBoxPassword" runat="server" TextMode="Password"></asp:TextBox>
                            </p>
							<p>
                                <br />
                                <asp:Button ID="Button1" runat="server" Text="Login" OnClick="Login_Click" />
                            </p>
						<hr>
						<p><small>Copyright (c) 2016 Peach Fuzzer, LLC. All rights reserved.</small></p>
						</td></tr>
					</tbody></table>
				</td></tr>
			</tbody></table>
		</center>
    </form>
</body>
</html>

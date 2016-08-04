<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="License.aspx.cs" Inherits="PeachDownloader.License" %>
<%@ Import Namespace="PeachDownloader" %>
<%
    // If license has not been validated, send to validate page
    if (!(bool)Session[SessionKeys.Authenticated])
    {
        Response.Redirect("Default.aspx", true);
    }
%>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head2" runat="server">
    <title>Terms and Conditions</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
		<p><strong>END-USER LICENSE AGREEMENT:</strong></p>
		<p><a href="http://www.peachfuzzer.com/eula/testsuite/">PEACH FUZZER™ TEST SUITE LICENSE</a></p>
		<p><a href="http://www.peachfuzzer.com/eula/developer-addendum"> PEACH FUZZER™ TEST SUITE – DEVELOPER ADDENDUM</a></p>



		<p><strong>BY CLICKING “I ACCEPT” YOU ACKNOWLEDGE THAT YOU HAVE READ, UNDERSTAND, AND AGREE TO BE BOUND BY THE TERMS ABOVE.</strong></p>
		<br />
		<p><br />CONTACT INFORMATION<br /><br />If you have any questions about this EULA, or if you want to contact Peach Fuzzer, LLC for 
			any reason, please direct all correspondence to: peach@peachfuzzer.com<br /><br /></p>

        <asp:Button ID="AcceptLicense" runat="server" Text="I ACCEPT" OnClick="AcceptLicense_Click" />&nbsp;&nbsp;<asp:Button ID="Fail" runat="server" Text="Cancel" OnClick="Fail_Click" />

        <br /><br /><br /><br /><br /><br /><br /><br /><br />

    </div>
    </form>
</body>
</html>

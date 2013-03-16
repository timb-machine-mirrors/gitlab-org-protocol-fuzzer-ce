<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PeachFarmMonitor.Home" Async="true" %>
<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
  <title>Peach Farm Monitor</title>
  <style type="text/css">
	  html,body {
	    height: 100%;
	    overflow: hidden;
	  }
	  body {
	    margin: 0;
      padding:0;
      background-color: white;
      font-family: 'Segoe UI', Arial;
      font-size: 12px;
      color: black;
      white-space:nowrap;
	  }
    #title
    {
      background: rgb(37, 160, 218);
      color: white;
      position:absolute;
      top:0;
      left:0;
      right:0;
      height:30px;
      font-size:x-large;
      font-family: 'Segoe UI', Arial;
    }
    .error
    {
      overflow:auto;
      color: black;
    }
    .error.alt
    {
      background-color: gainsboro;
    }
    .node
    {
      float:left;
      padding: 10px;
      margin: 2px;
    }
    .node.Running
    {
      background-color: lightgreen;
    }
    .node.Alive
    {
      background-color: lightblue;
    }
    td.fieldLabel
    {
      text-align: right;
    }
    #tabstrip
    {
      position: absolute;
      top:30px;
      left:0;
      right:0;
    }
    #toplevel
    {
      position: absolute;
      top:58px;
      bottom:0;
      left:0;
      right:0;
    }
    .job
    {
      height:120px;
      padding:10px;
      margin:2px;
      background-color:gainsboro;
    }
    .hidden
    {
      display: none;
    }
    #loadingLabel
    {
      position:absolute;
      right:0;
      padding-left: 4px;
      padding-right: 4px;
    }
  </style>
  <script type="text/javascript">
    function switchnodesview(source, args) {
      var text = args.get_item().get_text();
      var iconview = document.getElementById("lstNodes");
      var listview = document.getElementById("gridNodes");
      if (text == "Icon") {
        listview.className = "hidden";
        iconview.className = "";
      }
      else {
        iconview.className = "hidden";
        listview.className = "";
      }
    }
  </script>
</head>
<body>
  <form id="Form1" runat="server">
    <telerik:RadScriptManager ID="RadScriptManager1" runat="server" />
    <telerik:RadAjaxManager ID="RadAjaxManager1" OnAjaxRequest="RadAjaxManager1_AjaxRequest" runat="server">
      <AjaxSettings>
        <telerik:AjaxSetting AjaxControlID="monitorTimer">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="nodesGrid"/>
            <telerik:AjaxUpdatedControl ControlID="jobsGrid"/>
            <telerik:AjaxUpdatedControl ControlID="errorsGrid"/>
            <telerik:AjaxUpdatedControl ControlID="titlePanel" />
          </UpdatedControls>
        </telerik:AjaxSetting>
      </AjaxSettings>
    </telerik:RadAjaxManager>
    <div id="title">
      <asp:Panel ID="titlePanel" runat="server">
        Peach Farm Monitor:
        <asp:Label ID="lblHost" runat="server" />
        <asp:Label Text="LOADING" ID="loadingLabel" runat="server" BackColor="Green" />
      </asp:Panel>
    </div>
    <telerik:RadTabStrip ID="tabstrip" MultiPageID="toplevel" runat="server" SelectedIndex="0" Skin="Metro">
      <Tabs>
        <telerik:RadTab Text="Nodes"></telerik:RadTab>
        <telerik:RadTab Text="Jobs"></telerik:RadTab>
        <telerik:RadTab Text="Errors" Selected="True"></telerik:RadTab>
      </Tabs>
    </telerik:RadTabStrip>
    <telerik:RadMultiPage ID="toplevel" runat="server" SelectedIndex="0">
      <telerik:RadPageView ID="nodesPage" runat="server">
        <telerik:RadGrid ID="nodesGrid" runat="server" AutoGenerateColumns="false" AllowSorting="True" AllowFilteringByColumn="True" Skin="Metro" OnItemDataBound="nodesGrid_ItemDataBound">
          <MasterTableView>
            <Columns>
              <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
              <telerik:GridBoundColumn DataField="NodeName" HeaderText="Name" />
              <telerik:GridBoundColumn DataField="Stamp" HeaderText="Last Update" />
              <telerik:GridBoundColumn DataField="Tags" HeaderText="Tags" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
              <telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" />
            </Columns>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
      <telerik:RadPageView ID="jobsPage" runat="server">
        <telerik:RadGrid ID="jobsGrid" runat="server" AutoGenerateColumns="false" AllowSorting="true" AllowFilteringByColumn="true" Skin="Metro" OnItemDataBound="jobsGrid_ItemDataBound">
          <MasterTableView>
            <Columns>
              <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
              <telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" />
              <telerik:GridBoundColumn DataField="UserName" HeaderText="Owner" />
              <telerik:GridBoundColumn DataField="StartDate" HeaderText="Start Date" />
            </Columns>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
      <telerik:RadPageView ID="errorsPage" runat="server">
        <telerik:RadGrid ID="errorsGrid" runat="server" AutoGenerateColumns="false" AllowSorting="True" AllowFilteringByColumn="True" Skin="Metro" OnItemDataBound="errorsGrid_ItemDataBound">
          <MasterTableView>
            <Columns>
              <telerik:GridBoundColumn DataField="NodeName" HeaderText="Name" />
              <telerik:GridBoundColumn DataField="Stamp" HeaderText="Last Update" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
              <telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" />
            </Columns>
            <DetailItemTemplate>
              <asp:Label ID="ErrorMessage" runat="server" />
            </DetailItemTemplate>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
    </telerik:RadMultiPage>
    <asp:Panel ID="Panel1" runat="server">
      <asp:Timer ID="monitorTimer" runat="server" Interval="10000" OnTick="Tick">
      </asp:Timer>
    </asp:Panel>
  </form>
</body>
</html>

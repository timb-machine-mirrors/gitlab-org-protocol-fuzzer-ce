<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PeachFarmMonitor.Home" Async="true" Theme="DejaVu" %>
<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
  <title>Peach Farm Monitor</title>
  <telerik:RadStyleSheetManager ID="RadStyleSheetManager1" EnableStyleSheetCombine="true" runat="server"  />
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
      background: #333333;
      color: white;
      position:absolute;
      top:0;
      left:0;
      right:0;
      height:60px;
    }
    #titlePanel
    {
      font-size:x-large;
      font-family: 'Segoe UI', Arial;
    }
    #summaryPanel
    {
      padding-left: 4px;
    }
    #tabstrip
    {
      position: absolute;
      top:60px;
      left:0;
      right:0;
    }
    #toplevel
    {
      position: absolute;
      top:88px;
      bottom:0;
      left:0;
      right:0;
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
    #nodesGridPanel{ height:100%; }
    #jobsGridPanel{ height:100%; }
    #errorsGridPanel{ height:100%; }
  </style>
</head>
<body>
  <form id="Form1" runat="server">
    <telerik:RadScriptManager ID="RadScriptManager1" runat="server" OnAsyncPostBackError="RadScriptManager1_AsyncPostBackError" AllowCustomErrorsRedirect="true" AsyncPostBackTimeout="5000" />
    <telerik:RadAjaxManager ID="RadAjaxManager1" OnAjaxRequest="RadAjaxManager1_AjaxRequest" runat="server">
      <AjaxSettings>
        <telerik:AjaxSetting AjaxControlID="monitorTimer" EventName="Tick">
          <UpdatedControls>
        		<telerik:AjaxUpdatedControl ControlID="nodesGrid"/>
            <telerik:AjaxUpdatedControl ControlID="jobsGrid"/>
            <telerik:AjaxUpdatedControl ControlID="errorsGrid"/>
            <telerik:AjaxUpdatedControl ControlID="titlePanel" />
            <telerik:AjaxUpdatedControl ControlID="summaryPanel" />
          </UpdatedControls>
        </telerik:AjaxSetting>
        <telerik:AjaxSetting AjaxControlID="nodesGrid">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="nodesGrid" />
          </UpdatedControls>
        </telerik:AjaxSetting>
        <telerik:AjaxSetting AjaxControlID="jobsGrid">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="jobsGrid" />
          </UpdatedControls>
        </telerik:AjaxSetting>
        <telerik:AjaxSetting AjaxControlID="errorsGrid">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="errorsGrid" />
          </UpdatedControls>
        </telerik:AjaxSetting>
      </AjaxSettings>
    </telerik:RadAjaxManager>
    <div id="title">
      <asp:Panel ID="titlePanel" runat="server">
        <span style="padding-left: 4px">Peach Farm Monitor:</span>
        <asp:Label ID="lblHost" runat="server" />
        <asp:Label Text="LOADING" ID="loadingLabel" runat="server" BackColor="Green" />
      </asp:Panel>
      <asp:Panel ID="summaryPanel" runat="server">
        <asp:Table runat="server" CellPadding="2" CellSpacing="4">
          <asp:TableRow>
            <asp:TableCell>
              <asp:Label Text="Alive:" runat="server" />&nbsp;
              <asp:Label ID="aliveNodesLabel" runat="server" />
            </asp:TableCell>
            <asp:TableCell>
              <asp:Label Text="Running:" runat="server" />&nbsp;
              <asp:Label ID="runningNodesLabel" runat="server" />
            </asp:TableCell>
            <asp:TableCell>
              <asp:Label Text="Late:" runat="server" />&nbsp;
              <asp:Label ID="lateNodesLabel" runat="server" />
            </asp:TableCell>
          </asp:TableRow>
        </asp:Table>
      </asp:Panel>
    </div>
    <telerik:RadTabStrip ID="tabstrip" MultiPageID="toplevel" runat="server" SelectedIndex="0" EnableEmbeddedSkins="false" Skin="DejaVu">
      <Tabs>
        <telerik:RadTab Text="Nodes"></telerik:RadTab>
        <telerik:RadTab Text="Jobs"></telerik:RadTab>
        <telerik:RadTab Text="Errors"></telerik:RadTab>
      </Tabs>
    </telerik:RadTabStrip>
    <telerik:RadMultiPage runat="server" SelectedIndex="0" id="toplevel">
      <telerik:RadPageView ID="nodesPage" runat="server" Height="100%">
        <telerik:RadGrid ID="nodesGrid" runat="server" 
          AutoGenerateColumns="false" AllowSorting="True" 
          OnItemDataBound="nodesGrid_ItemDataBound" 
          OnSortCommand="nodesGrid_SortCommand" 
          Width="100%" Height="100%">
          <ClientSettings>
            <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
          </ClientSettings>
          <MasterTableView TableLayout="Fixed">
            <ColumnGroups>
              <telerik:GridColumnGroup HeaderText="Running Job Information" Name="JobInfo" />
            </ColumnGroups>
            <Columns>
              <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
              <telerik:GridBoundColumn DataField="NodeName" HeaderText="Name" />
              <telerik:GridBoundColumn DataField="Stamp" HeaderText="Last Update" />
              <telerik:GridBoundColumn DataField="Tags" HeaderText="Tags" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" ColumnGroupName="JobInfo" />
              <telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" ColumnGroupName="JobInfo" />
              <telerik:GridBoundColumn DataField="Seed" HeaderText="Seed" ColumnGroupName="JobInfo" />
              <telerik:GridBoundColumn DataField="Iteration" HeaderText="Iteration" ColumnGroupName="JobInfo" />
            </Columns>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
      <telerik:RadPageView ID="jobsPage" runat="server" Height="100%">
        <telerik:RadGrid ID="jobsGrid" runat="server" 
          AutoGenerateColumns="false" AllowSorting="true" AllowFilteringByColumn="false" 
          OnItemDataBound="jobsGrid_ItemDataBound" 
          OnSortCommand="jobsGrid_SortCommand"
          Height="100%" Width="100%">
          <ClientSettings>
            <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
          </ClientSettings>
          <MasterTableView TableLayout="Fixed">
            <Columns>
              <telerik:GridHyperLinkColumn Text="Generate Report" DataNavigateUrlFields="JobID" DataNavigateUrlFormatString="~/ReportViewer.aspx?jobid={0}" Target="_blank" AllowSorting="false" />
              <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
              <telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" />
              <telerik:GridBoundColumn DataField="UserName" HeaderText="Owner" />
              <telerik:GridBoundColumn DataField="StartDate" HeaderText="Start Date" />
              <telerik:GridHyperLinkColumn HeaderText="Faults" DataTextField="FaultCount" DataTextFormatString="View Faults ({0})" DataNavigateUrlFields="JobID" DataNavigateUrlFormatString="~/JobDetail.aspx?jobid={0}" Target="_blank" SortExpression="FaultCount"/>
            </Columns>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
      <telerik:RadPageView ID="errorsPage" runat="server" Height="100%">
        <telerik:RadGrid ID="errorsGrid" runat="server" 
          AutoGenerateColumns="false" AllowSorting="True" AllowFilteringByColumn="False"
          OnItemDataBound="errorsGrid_ItemDataBound" 
          OnSortCommand="errorsGrid_SortCommand"
          Width="100%" Height="100%">
          <ClientSettings>
            <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
          </ClientSettings>
          <MasterTableView>
            <Columns>
              <telerik:GridBoundColumn DataField="NodeName" HeaderText="Name" />
              <telerik:GridBoundColumn DataField="Stamp" HeaderText="Last Update" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
              <telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" />
            </Columns>
            <DetailItemTemplate>
              <asp:TextBox ID="ErrorMessage" TextMode="MultiLine" BorderStyle="None" BorderWidth="0" ReadOnly="true" Wrap="true" Font-Names="Consolas, Courier New" Font-Size="Small" runat="server" Width="100%" Rows="25" />
            </DetailItemTemplate>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
    </telerik:RadMultiPage>
    <asp:Panel ID="Panel1" runat="server" Width="0" Height="0">
      <asp:Timer ID="monitorTimer" runat="server" Interval="10000" OnTick="Tick">
      </asp:Timer>
    </asp:Panel>
  </form>
</body>
</html>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PeachFarmMonitor.Home" Async="true" Theme="DejaVu" %>
<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>

<!DOCTYPE html>

<html>
<head id="Head1" runat="server">
  <title>Peach Farm Monitor</title>
  <telerik:RadStyleSheetManager ID="RadStyleSheetManager1" EnableStyleSheetCombine="true" runat="server"  />
	<!--
	-->
	<meta http-equiv="refresh" content="300" />
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
      background-color: #dcdcdc;
      color: black;
      /*
      background: #333333;
      color: white;
      */
      position:absolute;
      top:0;
      left:0;
      right: 0;
      height:32px;
    }
    #titlePanel
    {
      float: left;
      right: 300px;
      font-size:x-large;
      font-family: 'Segoe UI', Arial;
    }
    #brand
    {
      float:right;
      margin-top: -1px;
      height:32px;
      width:300px;
    }
    #summaryPanel
    {
      padding-left: 4px;
    }
    #tabstrip
    {
      position: absolute;
      top:32px;
      left:0;
      right:0;
    }
    #toplevel
    {
      position: absolute;
      top:60px;
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
      float:right;
      padding-left: 4px;
      padding-right: 4px;
    }
    #nodesGridPanel{ height:100%; }
    #jobsGridPanel{ height:100%; }
    #errorsGridPanel{ height:100%; }
		input[type='radio']
		{
			border-color: #f57e20;
		}
  </style>
	<script>
		function SelectBy(param) {
			var lstCount = document.getElementById("startJobWindow_C_lstCount");
			var lstTags = document.getElementById("startJobWindow_C_lstTags");

			if (param == "count") {
				lstCount.style.display = "block";
				lstTags.style.display = "none";
			}
			else {
				lstTags.style.display = "block";
				lstCount.style.display = "none";
			}
		}
	</script>
</head>
<body>
  <form id="Form1" runat="server">
    <telerik:RadScriptManager ID="RadScriptManager1" runat="server" AllowCustomErrorsRedirect="true" AsyncPostBackTimeout="5000" EnablePartialRendering="true" />
    <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server">
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
        <span style="padding-left: 4px">Peach Farm Monitor</span>
        <asp:Label ID="lblHost" runat="server" Visible="false" />
        <asp:Label Text="LOADING" ID="loadingLabel" runat="server" BackColor="Green" />
      </asp:Panel>
      <asp:Table ID="brand" runat="server" CellPadding="0" CellSpacing="0" BorderWidth="0">
        <asp:TableRow>
          <asp:TableCell HorizontalAlign="Center" VerticalAlign="Middle">
            <span>© 2013 Déjà vu Security -</span>
            <a href="http://www.dejavusecurity.com/contact.html" target="_blank">Contact</a>
          </asp:TableCell>
          <asp:TableCell HorizontalAlign="Center" VerticalAlign="Middle">
            <img src="dejavulogo.jpg" height="30" style="display:inline-block;border:0" />
          </asp:TableCell>
        </asp:TableRow>
      </asp:Table>
    </div>
    <telerik:RadTabStrip ID="tabstrip" MultiPageID="toplevel" runat="server" SelectedIndex="0" EnableEmbeddedSkins="false" Skin="DejaVu">
      <Tabs>
        <telerik:RadTab Text="Jobs"></telerik:RadTab>
        <telerik:RadTab Text="Nodes"></telerik:RadTab>
        <telerik:RadTab Text="Errors"></telerik:RadTab>
      </Tabs>
    </telerik:RadTabStrip>
    <telerik:RadMultiPage runat="server" SelectedIndex="0" id="toplevel">
      <telerik:RadPageView ID="jobsPage" runat="server" Height="100%">
        <asp:Panel ID="jobActionPanel" runat="server" Visible="false">
          <asp:Table ID="Table2" runat="server" CellPadding="2" CellSpacing="4">
            <asp:TableRow>
              <asp:TableCell>
                <telerik:RadButton Text="Start New Job" ID="btnOpenStartJobWindow" runat="server"/>
              </asp:TableCell>
              <asp:TableCell>
                <telerik:RadButton Text="Stop Job" ID="btnStopJob" runat="server"/>
              </asp:TableCell>
              <asp:TableCell>
              </asp:TableCell>
            </asp:TableRow>
          </asp:Table>
        </asp:Panel>
				<telerik:RadGrid ID="jobsGrid" runat="server" 
          AutoGenerateColumns="false" AllowSorting="true"
					AllowFilteringByColumn="false" 
          OnItemDataBound="jobsGrid_ItemDataBound" 
					OnSortCommand="jobsGrid_SortCommand"
					OnItemCommand="jobsGrid_ItemCommand"
					OnNeedDataSource="jobsGrid_NeedDataSource"
					OnDetailTableDataBind="jobsGrid_DetailTableDataBind"
					ImagesPath="~/App_Themes/DejaVu/Grid"
          Height="100%" Width="100%">
          <ClientSettings>
            <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
						<Selecting AllowRowSelect="true" CellSelectionMode="None" />
						<ClientEvents OnRowSelected="" />
          </ClientSettings>
          <MasterTableView HierarchyLoadMode="ServerBind" TableLayout="Fixed" ViewStateMode="Enabled">
            <Columns>
							<telerik:GridButtonColumn Text="Stop Job" ButtonType="LinkButton" CommandName="StopJob" ConfirmDialogType="RadWindow" ConfirmTextFields="JobID" ConfirmTextFormatString="Are you sure you want to stop Job {0}?" UniqueName="StopJobButton" Visible="false" />
              <telerik:GridBoundColumn DataField="Status" HeaderText="Status" />
              <telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
              <telerik:GridBoundColumn DataField="Pit.FileName" HeaderText="Pit File" />
              <telerik:GridBoundColumn DataField="UserName" HeaderText="Owner" />
              <telerik:GridBoundColumn DataField="StartDate" HeaderText="Start Date" />
              <telerik:GridBoundColumn DataField="IterationCount" HeaderText="Iterations" />
              <telerik:GridHyperLinkColumn HeaderText="Job Input" Text="Download" DataNavigateUrlFields="ZipFile" DataNavigateUrlFormatString="~/GetJobOutput.aspx?file={0}" Target="_blank" AllowSorting="false"/>
              <telerik:GridHyperLinkColumn HeaderText="Faults" DataTextField="FaultCount" DataTextFormatString="View Faults ({0})" DataNavigateUrlFields="JobID" DataNavigateUrlFormatString="~/JobDetail.aspx?jobid={0}" Target="_blank" SortExpression="FaultCount" AllowSorting="false"/>
							<telerik:GridTemplateColumn HeaderText="PDF Report" Visible="true">
								<ItemTemplate>
									<asp:HyperLink ID="linkDownloadReport" runat="server"/>
								</ItemTemplate>
							</telerik:GridTemplateColumn>
            </Columns>

          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
      <telerik:RadPageView ID="nodesPage" runat="server" Height="100%">
        <asp:Panel ID="summaryPanel" runat="server">
          <asp:Table ID="Table1" runat="server" CellPadding="2" CellSpacing="4">
            <asp:TableRow>
              <asp:TableCell>
                <asp:Label ID="Label1" Text="Alive:" runat="server" />&nbsp;
                <asp:Label ID="aliveNodesLabel" runat="server" />
              </asp:TableCell><asp:TableCell>
                <asp:Label ID="Label2" Text="Running:" runat="server" />&nbsp;
                <asp:Label ID="runningNodesLabel" runat="server" />
              </asp:TableCell><asp:TableCell>
                <asp:Label ID="Label3" Text="Late:" runat="server" />&nbsp;
                <asp:Label ID="lateNodesLabel" runat="server" />
              </asp:TableCell></asp:TableRow></asp:Table></asp:Panel><telerik:RadGrid ID="nodesGrid" runat="server"
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
              <telerik:RadPanelBar ID="messagePanel" runat="server" Width="100%" PersistStateInCookie="true" >
                <Items>
                  <telerik:RadPanelItem Text="Error Message">
                    <ContentTemplate>
                      <asp:TextBox ID="ErrorMessage" TextMode="MultiLine" BorderStyle="None" BorderWidth="0" ReadOnly="true" Wrap="true" Font-Names="Consolas, Courier New" Font-Size="Small" runat="server" Width="100%" Rows="25" />
                    </ContentTemplate>
                  </telerik:RadPanelItem>
                </Items>
              </telerik:RadPanelBar>
            </DetailItemTemplate>
          </MasterTableView>
        </telerik:RadGrid>
      </telerik:RadPageView>
    </telerik:RadMultiPage>
    <asp:Panel ID="Panel1" runat="server" Width="0" Height="0">
				<telerik:RadWindow ID="startJobWindow" runat="server" Width="360px" Height="360px" VisibleOnPageLoad="false" OpenerElementID="btnOpenStartJobWindow" Modal="True" Title="Start New Job" AutoSize="True">
					<ContentTemplate>
						<div style="padding: 10px; text-align: center;">
							<asp:Table ID="Table3" runat="server">
								<asp:TableRow>
									<asp:TableCell HorizontalAlign="Right">
										Pit File:
									</asp:TableCell>
									<asp:TableCell HorizontalAlign="Left">
									</asp:TableCell>
								</asp:TableRow>
								<asp:TableRow>
									<asp:TableCell HorizontalAlign="Right">
										Other Files:
									</asp:TableCell>
									<asp:TableCell HorizontalAlign="Left">
									</asp:TableCell>
								</asp:TableRow>
								<asp:TableRow>
									<asp:TableCell HorizontalAlign="Left" ColumnSpan="2">
										<!--
										<asp:RadioButtonList ID="chkSelectBy" runat="server" ClientIDMode="Static" RepeatDirection="Horizontal">
											<asp:ListItem Selected="True" Value="count" Text="Count" />
											<asp:ListItem Value="tags" Text="Tags" />
										</asp:RadioButtonList>
										-->
										<asp:RadioButton GroupName="SelectBy" ID="chkSelectByCount" Text="Count" Checked="true" runat="server" ClientIDMode="Static" />
										<asp:RadioButton GroupName="SelectBy" ID="chkSelectByTags" Text="Tags" runat="server" ClientIDMode="Static" />
									</asp:TableCell>
								</asp:TableRow>
								<asp:TableRow>
									<asp:TableCell HorizontalAlign="Left" ColumnSpan="2">
										<telerik:RadDropDownList ID="lstCount" runat="server" />
										<asp:CheckBoxList ID="lstTags" runat="server" />
									</asp:TableCell>
								</asp:TableRow>
								<asp:TableRow>
									<asp:TableCell ColumnSpan="2" HorizontalAlign="Right">
										<telerik:RadButton ID="btnCancel" Text="Cancel" runat="server" />
										<telerik:RadButton ID="btnStartJob" Text="Start" runat="server" />
									</asp:TableCell>
								</asp:TableRow>
							</asp:Table>
						</div>
					</ContentTemplate>
				</telerik:RadWindow>
      <asp:Timer ID="monitorTimer" runat="server" Interval="10000" OnTick="Tick" Enabled="true" />
    </asp:Panel>
  </form>
</body>
</html>

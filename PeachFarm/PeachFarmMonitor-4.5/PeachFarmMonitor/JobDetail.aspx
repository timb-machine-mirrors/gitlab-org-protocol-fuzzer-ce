<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobDetail.aspx.cs" Inherits="PeachFarmMonitor.JobDetail" Theme="DejaVu" EnableEventValidation="false" ValidateRequest="false" %>

<!DOCTYPE html>

<html>
<head runat="server">
  <telerik:RadStyleSheetManager ID="RadStyleSheetManager1" runat="server" EnableStyleSheetCombine="true" />
  <title></title>
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
      background: #dcdcdc;
      color: black;
      position:absolute;
      top:0;
      left:0;
      right:300px;
      height:32px;
      font-size:x-large;
      font-family: 'Segoe UI', Arial;
    }
    #linkbar
    {
      position: absolute;
      top:32px;
      left:0;
      right:150px;
      height:20px;
    }
    .linkbarItem
    {
      color: #f57e20;
      font-weight: bold;
    }
    #gridcontainer
    {
      position: absolute;
      top:52px;
      left:0;
      right:0;
      bottom:0px;
    }
    #brand
    {
      background-color: #dcdcdc;
      float:right;
      height:32px;
      width:300px;
      margin-top: -1px;
    }

    .RadGrid_DejaVu .rgPageFirst
    {
      display: none;
    }
    .RadGrid_DejaVu .rgPageLast
    {
      display: none;
    }
    .RadGrid_DejaVu .rgInfoPart
    {
      /* display:none; */
    }
    #faultsGridPanel,#faultBucketsGridPanel
    {
      height: 100%;
    }
    .rgExpandCol
    {
      width: 70px;
    }

  </style>
</head>
<body>
  <form id="form1" runat="server">
    <telerik:RadScriptManager ID="RadScriptManager1" runat="server" />
    <telerik:RadAjaxLoadingPanel ID="loadingPanel" runat="server"  />
    <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server" >
      <AjaxSettings>
        <telerik:AjaxSetting AjaxControlID="faultBucketsGrid" EventName="ItemCommand">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="faultBucketsGrid" LoadingPanelID="loadingPanel" />
            <telerik:AjaxUpdatedControl ControlID="faultsGrid" LoadingPanelID="loadingPanel" />
          </UpdatedControls>
        </telerik:AjaxSetting>
        <telerik:AjaxSetting AjaxControlID="faultsGrid" EventName="NeedDataSource">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="faultsGrid" LoadingPanelID="loadingPanel" />
          </UpdatedControls>
        </telerik:AjaxSetting>
      </AjaxSettings>
    </telerik:RadAjaxManager>
    <div id="title">
      <asp:Panel ID="titlePanel" runat="server">
        <span style="padding-left: 4px">Job Detail:</span>
        <asp:Label ID="lblJobID" runat="server" />
      </asp:Panel>
    </div>
    <div id="linkbar">
      [
      <asp:HyperLink CssClass="linkbarItem" ID="downloadInputLink" runat="server" Text="Download Job Input" Target="_blank" />&nbsp;|
      <asp:HyperLink CssClass="linkbarItem" ID="downloadOutputLink" runat="server" Text="Download Job Output" Target="_blank" />&nbsp;]
			<asp:HyperLink CssClass="linkbarItem" ID="viewReportLink" runat="server" Text="View Printable Report" Target="_blank" Visible="false" />

    </div>
    <asp:Table ID="brand" runat="server" CellPadding="0" CellSpacing="0">
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

    <div id="gridcontainer">
      <telerik:RadSplitter Height="100%" Width="100%" runat="server" >
        <telerik:RadPane runat="server" Scrolling="None" Width="30%" ID="faultBucketsPane"> 
          <telerik:RadGrid 
            ID="faultBucketsGrid" runat="server"
            Width="100%" Height="100%"
            OnItemCommand="faultBucketsGrid_ItemCommand"
            AutoGenerateColumns="False" AutoGenerateHierarchy="False" CellSpacing="0" GridLines="None">
            <ClientSettings>
              <Resizing AllowColumnResize="false" />
              <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
            </ClientSettings>
            <MasterTableView 
              AllowPaging="false" PageSize="30" AllowCustomPaging="true" VirtualItemCount="1000000"
              Caption="Fault Groups" 
              NoDetailRecordsText="No faults for this job."
              HierarchyLoadMode="ServerOnDemand">
              <PagerStyle PageSizeControlType="None" Mode="NextPrev" AlwaysVisible="false"  />
              <Columns>
                <telerik:GridBoundColumn DataField="FolderName" HeaderText="Fault" Resizable="false"/>
                <telerik:GridBoundColumn DataField="FaultCount" HeaderText="Count" Resizable="false" ItemStyle-Width="50px" HeaderStyle-Width="50px"/>
                <telerik:GridButtonColumn CommandName="FaultBucketSelect" Text="View Faults &gt;" Resizable="false" ItemStyle-Width="90px" HeaderStyle-Width="90px"/>
              </Columns>
            </MasterTableView>
          </telerik:RadGrid>
        </telerik:RadPane>
        <telerik:RadSplitBar runat="server" />
        <telerik:RadPane runat="server" Scrolling="None">
	        <div>
		        Find Iteration:
						<telerik:RadSearchBox ID="faultSearch" EnableAutoComplete="False" ShowSearchButton="True" runat="server">
						</telerik:RadSearchBox>
	        </div>
          <telerik:RadGrid 
            ID="faultsGrid" runat="server" 
            Width="100%" Height="100%" ImagesPath="~/App_Themes/DejaVu/Grid"
            AutoGenerateColumns="False" AutoGenerateHierarchy="False" CellSpacing="0" GridLines="None" AllowFilteringByColumn="False">
            <ClientSettings>
              <Resizing AllowColumnResize="false" />
              <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
            </ClientSettings>
            <MasterTableView
              AllowPaging="true" PageSize="15" AllowCustomPaging="true" VirtualItemCount="1000000"
              Caption="Faults"
              NoDetailRecordsText="No faults for this group."
              HierarchyLoadMode="ServerBind">
              <PagerStyle PageSizeControlType="None" Mode="NextPrevNumericAndAdvanced" AlwaysVisible="true"  />
              <ExpandCollapseColumn Visible="True" Resizable="true" />
              <Columns>
                <telerik:GridBoundColumn DataField="Title" AllowFiltering="False" HeaderText="Title" />
                <telerik:GridBoundColumn DataField="Exploitability" AllowFiltering="False" HeaderText="Exploitability" />
                <telerik:GridBoundColumn DataField="DetectionSource" AllowFiltering="False" HeaderText="Source" />
                <telerik:GridBoundColumn DataField="MajorHash" HeaderText="Major Hash" AllowFiltering="False" ItemStyle-Width="80px" HeaderStyle-Width="80px" />
                <telerik:GridBoundColumn DataField="MinorHash" HeaderText="Minor Hash" AllowFiltering="False" ItemStyle-Width="80px" HeaderStyle-Width="80px" />
                <telerik:GridBoundColumn DataField="Iteration" HeaderText="Iteration" AllowFiltering="False" ItemStyle-Width="60px" HeaderStyle-Width="60px" />
                <telerik:GridBoundColumn DataField="IsReproduction" HeaderText="Is Reproduction" AllowFiltering="False" ItemStyle-Width="100px" HeaderStyle-Width="100px" />
								<telerik:GridHyperLinkColumn Text="Link/Print" DataNavigateUrlFields="ID" DataNavigateUrlFormatString="~/FaultDetail.aspx?faultid={0}"/>
              </Columns>
              <DetailTables>
                <telerik:GridTableView  BorderColor="#f57e20" BorderWidth="2"
									Caption="Description" DataMember="Description" HierarchyLoadMode="ServerBind" ShowHeader="false">
                  <Columns>
                    <telerik:GridTemplateColumn DataField="Description">
                      <ItemTemplate>
                        <asp:TextBox Text="<%# Container.DataItem %>" TextMode="MultiLine" BorderStyle="None" BorderWidth="0" ReadOnly="true" Wrap="true" Font-Names="Consolas, Courier New" Font-Size="Small" runat="server" Width="100%" Rows="10" />
                      </ItemTemplate>
                    </telerik:GridTemplateColumn>
                  </Columns>
                </telerik:GridTableView>
                <telerik:GridTableView BorderColor="#f57e20" BorderWidth="2"
                  Caption="Generated Files" DataMember="GeneratedFileViewModels" 
                  HierarchyLoadMode="ServerBind"
                  NoDetailRecordsText="No generated files for this fault.">
                  <Columns>
                    <telerik:GridHyperLinkColumn DataTextField="Name" DataNavigateUrlFields="GridFSLocation" DataNavigateUrlFormatString="~/GetJobOutput.aspx?file={0}" HeaderText="File" Target="_blank" />
                  </Columns>
                </telerik:GridTableView>
              </DetailTables>
            </MasterTableView>
          </telerik:RadGrid>
        </telerik:RadPane>
      </telerik:RadSplitter>
    </div>
  </form>
</body>
</html>

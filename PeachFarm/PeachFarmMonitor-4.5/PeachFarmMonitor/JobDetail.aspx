<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobDetail.aspx.cs" Inherits="PeachFarmMonitor.JobDetail" Theme="DejaVu" EnableEventValidation="false" ValidateRequest="false" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <telerik:RadStyleSheetManager ID="RadStyleSheetManager1" runat="server" />
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
      background: #333333;
      color: white;
      position:absolute;
      top:0;
      left:0;
      right:0;
      height:30px;
      font-size:x-large;
      font-family: 'Segoe UI', Arial;
    }
    #linkbar
    {
      position: absolute;
      top:30px;
      left:0;
      right:0;
      height:20px;
    }
    #gridcontainer
    {
      position: absolute;
      top:50px;
      left:0;
      right:0;
      bottom:0;
    }
  </style>
  <script type="text/javascript">
    function GridCreated(sender, args) {
      var scrollArea = sender.GridDataDiv;
      var parent = $get("gridContainer");
      var gridHeader = sender.GridHeaderDiv;
      scrollArea.style.height = parent.clientHeight -
        gridHeader.clientHeight + "px";
    }
  </script>
</head>
<body>
  <form id="form1" runat="server">
    <telerik:RadScriptManager ID="RadScriptManager1" runat="server" />
    <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server" />
    <div id="title">
      <asp:Panel ID="titlePanel" runat="server">
        <span style="padding-left: 4px">Job Detail:</span>
        <asp:Label ID="lblJobID" runat="server" />
      </asp:Panel>
    </div>
    <div id="linkbar">
      [&nbsp;
      <asp:HyperLink ID="downloadOutputLink" runat="server" Text="Download Job Output" Target="_blank" />&nbsp;|&nbsp;
      <asp:HyperLink ID="viewReportLink" runat="server" Text="View Printable Report" Target="_blank" />&nbsp;]
    </div>
    <div id="gridcontainer">
      <telerik:RadGrid 
        ID="iterationsGrid" runat="server"
        Width="100%" Height="100%"
        OnDetailTableDataBind="iterationsGrid_DetailTableDataBind" 
        OnItemDataBound="iterationsGrid_ItemDataBound" 
        AutoGenerateColumns="false" AutoGenerateHierarchy="true">
        <ClientSettings>
          <Scrolling AllowScroll="true" SaveScrollPosition="true" UseStaticHeaders="true" />
        </ClientSettings>
        <MasterTableView DataMember="Iterations" Caption="Iterations" HierarchyLoadMode="ServerOnDemand">
          <Columns>
            <telerik:GridBoundColumn DataField="IterationNumber" HeaderText="Iteration" />
            <telerik:GridBoundColumn DataField="TestName" HeaderText="Test" />
            <telerik:GridBoundColumn DataField="SeedNumber" HeaderText="Seed" />
            <telerik:GridBoundColumn DataField="Stamp" HeaderText="Stamp" />
            <telerik:GridBoundColumn DataField="NodeName" HeaderText="Node" />
          </Columns>
          <DetailTables>
            <telerik:GridTableView Caption="Faults" DataMember="Faults" HierarchyLoadMode="ServerBind">
              <Columns>
                <telerik:GridBoundColumn DataField="Title" HeaderText="Title" />
                <telerik:GridBoundColumn DataField="DetectionSource" HeaderText="Source" />
                <telerik:GridBoundColumn DataField="Exploitability" HeaderText="Exploitability" />
                <telerik:GridBoundColumn DataField="MajorHash" HeaderText="Major Hash" />
                <telerik:GridBoundColumn DataField="MinorHash" HeaderText="Minor Hash" />
                <telerik:GridBoundColumn DataField="IsReproduction" HeaderText="Is Reproduction" />
              </Columns>
              <DetailItemTemplate>
                <telerik:RadPanelBar ID="descriptionPanel" runat="server" Width="100%">
                  <Items>
                    <telerik:RadPanelItem Text="Description" Expanded="false">
                      <ContentTemplate>
                        <asp:TextBox ID="descriptionLabel" TextMode="MultiLine" BorderStyle="None" BorderWidth="0" ReadOnly="true" Wrap="true" Font-Names="Consolas, Courier New" Font-Size="Small" runat="server" Width="100%" Rows="25" />
                      </ContentTemplate>
                    </telerik:RadPanelItem>
                  </Items>
                </telerik:RadPanelBar>
              </DetailItemTemplate>
              <DetailTables>
                <telerik:GridTableView Caption="State Model" DataMember="StateModel" HierarchyLoadMode="ServerBind">
                  <Columns>
                    <telerik:GridBoundColumn DataField="ActionName" HeaderText="Action" />
                    <telerik:GridBoundColumn DataField="ActionType" HeaderText="Type" />
                    <telerik:GridBoundColumn DataField="Parameter" HeaderText="Parameter" />
                    <telerik:GridHyperLinkColumn Text="Download File" DataNavigateUrlFields="DataPath" DataNavigateUrlFormatString="~/jobArchive/{0}" HeaderText="File" Target="_blank" />
                  </Columns>
                </telerik:GridTableView>
                <telerik:GridTableView Caption="Collected Data" DataMember="CollectedData" HierarchyLoadMode="ServerBind">
                  <Columns>
                    <telerik:GridBoundColumn DataField="Key" HeaderText="Key" />
                    <telerik:GridHyperLinkColumn Text="Download File" DataNavigateUrlFields="DataPath" DataNavigateUrlFormatString="~/jobArchive/{0}" HeaderText="File" Target="_blank" />
                  </Columns>
                </telerik:GridTableView>
              </DetailTables>
            </telerik:GridTableView>
          </DetailTables>
        </MasterTableView>
      </telerik:RadGrid>
    </div>
  </form>
</body>
</html>

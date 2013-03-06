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
      position:absolute;
      top:0;
      left:0;
      right:0;
      height:30px;
      font-size:x-large;
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
    #jobsnodeserrors
    {
      position: absolute;
      top:30px;
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
  </style>
</head>
<body>
  <form id="Form1" runat="server">
    <telerik:RadScriptManager ID="RadScriptManager1" runat="server" />
    <telerik:RadAjaxManager ID="RadAjaxManager1" OnAjaxRequest="RadAjaxManager1_AjaxRequest" runat="server">
      <AjaxSettings>
        <telerik:AjaxSetting AjaxControlID="monitorTimer">
          <UpdatedControls>
            <telerik:AjaxUpdatedControl ControlID="lstNodes"/>
            <telerik:AjaxUpdatedControl ControlID="lstErrors"/>
            <telerik:AjaxUpdatedControl ControlID="lstJobs"/>
            <telerik:AjaxUpdatedControl ControlID="lstInactiveJobs"/>
            <telerik:AjaxUpdatedControl ControlID="titlePanel" />
          </UpdatedControls>
        </telerik:AjaxSetting>
      </AjaxSettings>
    </telerik:RadAjaxManager>
    <div id="title">
      <asp:Panel ID="titlePanel" runat="server">
        Peach Farm Monitor:
        <asp:Label ID="lblHost" runat="server" />
        <asp:Label Text="LOADING" ID="loadingLabel" runat="server" ForeColor="Green" />
      </asp:Panel>
    </div>
    <div id="jobsnodeserrors">
      <telerik:RadSplitter ID="RadSplitter1" Orientation="Horizontal" runat="server" Height="100%" Width="100%" Skin="Metro">
        <telerik:RadPane ID="RadPane3" runat="server">
          <telerik:RadSplitter ID="RadSplitter2" runat="server">
            <telerik:RadPane ID="RadPane1" runat="server" Width="220">
              <telerik:RadSplitter ID="RadSplitter3" runat="server" Orientation="Horizontal">
                <telerik:RadPane runat="server">
                  <telerik:RadListView ID="lstJobs" runat="server" ItemPlaceholderID="jobHolder">
                    <LayoutTemplate>
                      <fieldset id="lstJobs" style="color: black;">
                        <legend>Jobs</legend>
                        <asp:Panel ID="jobHolder" runat="server" />
                      </fieldset>
                    </LayoutTemplate>
                    <ItemTemplate>
                      <table class="job">
                        <tr>
                          <td class="fieldLabel">Job ID:</td>
                          <td><%# Eval("JobID") %></td>
                        </tr>
                        <tr>
                          <td class="fieldLabel">Pit:</td>
                          <td><%# Eval("PitFileName") %></td>
                        </tr>
                        <tr>
                          <td class="fieldLabel">User:</td>
                          <td><%# Eval("UserName") %></td>
                        </tr>
                        <tr>
                          <td class="fieldLabel">Start:</td>
                          <td><%# ((DateTime) Eval("StartDate")).ToLocalTime() %></td>
                        </tr>
                        <tr>
                          <td style="text-align:center" colspan="2">
                            <a href="ReportViewer.aspx?jobid=<%# Eval("JobID") %>" target="_blank">
                              View Report
                            </a>
                          </td>
                        </tr>
                      </table>
                    </ItemTemplate>
                  </telerik:RadListView>
                </telerik:RadPane>
                <telerik:RadSplitBar ID="RadSplitBar2" runat="server" />
                <telerik:RadPane ID="RadPane5" runat="server">
                  <telerik:RadListView ID="lstInactiveJobs" runat="server" ItemPlaceholderID="inactiveJobHolder">
                    <LayoutTemplate>
                      <fieldset id="lstInactiveJobs" style="color: black;">
                        <legend>Inactive Jobs</legend>
                        <asp:Panel ID="inactiveJobHolder" runat="server" />
                      </fieldset>
                    </LayoutTemplate>
                    <ItemTemplate>
                      <table class="job">
                        <tr>
                          <td class="fieldLabel">Job ID:</td>
                          <td><%# Eval("JobID") %></td>
                        </tr>
                        <tr>
                          <td class="fieldLabel">Pit:</td>
                          <td><%# Eval("PitFileName") %></td>
                        </tr>
                        <tr>
                          <td class="fieldLabel">User:</td>
                          <td><%# Eval("UserName") %></td>
                        </tr>
                        <tr>
                          <td class="fieldLabel">Start:</td>
                          <td><%# ((DateTime) Eval("StartDate")).ToLocalTime() %></td>
                        </tr>
                        <tr>
                          <td style="text-align:center" colspan="2">
                            <a href="ReportViewer.aspx?jobid=<%# Eval("JobID") %>" target="_blank">
                              View Report
                            </a>
                          </td>
                        </tr>
                      </table>
                    </ItemTemplate>
                  </telerik:RadListView>
                </telerik:RadPane>
              </telerik:RadSplitter>
            </telerik:RadPane>
            <telerik:RadPane ID="RadPane2" runat="server" Width="50%">
              <telerik:RadListView ID="lstNodes" runat="server" ItemPlaceholderID="nodeHolder">
                <LayoutTemplate>
                  <fieldset id="lstNodes" style="color: black">
                    <legend>Nodes</legend>
                    <asp:Panel ID="nodeHolder" runat="server" ScrollBars="Vertical" />
                  </fieldset>
                </LayoutTemplate>
                <ItemTemplate>
                  <table class="node <%# Eval("Status").ToString() %>">
                    <tr>
                      <td class="fieldLabel">Name:</td>
                      <td><%# Eval("NodeName") %></td>
                    </tr>
                    <tr>
                      <td class="fieldLabel">Stamp:</td>
                      <td><%# Eval("Stamp") %></td>
                    </tr>
                    <tr>
                      <td class="fieldLabel">Tags:</td>
                      <td><%# Eval("Tags") %></td>
                    </tr>
                    <tr>
                      <td class="fieldLabel">Status:</td>
                      <td><%# Eval("Status") %></td>
                    </tr>
                    <tr>
                      <td style="height:1px;background-color:gray" colspan="2"/>
                    </tr>
                    <tr>
                      <td class="fieldLabel">Job ID:</td>
                      <td><%# Eval("JobID") %></td>
                    </tr>
                    <tr>
                      <td class="fieldLabel">Pit:</td>
                      <td><%# Eval("PitFileName") %></td>
                    </tr>
                  </table>
                </ItemTemplate>
              </telerik:RadListView>
            </telerik:RadPane>
          </telerik:RadSplitter>
        </telerik:RadPane>
        <telerik:RadSplitBar ID="RadSplitBar1" runat="server" />
        <telerik:RadPane ID="RadPane4" runat="server" Height="50">
          <telerik:RadListView ID="lstErrors" runat="server" ItemPlaceholderID="errorHolder">
            <LayoutTemplate>
              <fieldset id="lstErrors">
                <legend>Errors Log</legend>
                <asp:Panel ID="errorHolder" runat="server"/>
              </fieldset>
            </LayoutTemplate>
            <AlternatingItemTemplate>
              <table class="error alt">
                <tr>
                  <td>
                    Name: <%# Eval("NodeName") %>
                  </td>
                  <td>
                    Stamp: <%# ((DateTime) Eval("Stamp")).ToLocalTime() %>
                  </td>
                </tr>
                <tr>
                  <td>
                    Job ID: <%# Eval("JobID") %>
                  </td>
                  <td>
                    Pit: <%# Eval("PitFileName") %>
                  </td>
                </tr>
                <tr>
                  <td colspan="2" style="white-space:normal;column-span:all;padding:5px"><%# Eval("ErrorMessage") %></td>
                </tr>
              </table>
            </AlternatingItemTemplate>
            <ItemTemplate>
              <table class="error">
                <tr>
                  <td>
                    Name: <%# Eval("NodeName") %>
                  </td>
                  <td>
                    Stamp: <%# ((DateTime) Eval("Stamp")).ToLocalTime() %>
                  </td>
                </tr>
                <tr>
                  <td>
                    Job ID: <%# Eval("JobID") %>
                  </td>
                  <td>
                    Pit: <%# Eval("PitFileName") %>
                  </td>
                </tr>
                <tr>
                  <td colspan="2" style="white-space:normal;column-span:all;margin:5px"><%# Eval("ErrorMessage") %></td>
                </tr>
              </table>
            </ItemTemplate>
          </telerik:RadListView>
        </telerik:RadPane>
      </telerik:RadSplitter>
    </div>
    <asp:Panel ID="Panel1" runat="server">
      <asp:Timer ID="monitorTimer" runat="server" Interval="10000" OnTick="Tick">
      </asp:Timer>
    </asp:Panel>
  </form>
</body>
</html>

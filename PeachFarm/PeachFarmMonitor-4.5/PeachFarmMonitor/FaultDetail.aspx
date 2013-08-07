<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FaultDetail.aspx.cs" Inherits="PeachFarmMonitor.FaultDetail" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title></title>
	<style>
		.mono {
			font-family: monospace;
			font-size: smaller;
			white-space: pre-wrap;
		}
		.monowrap {
			font-family: monospace;
			font-size: smaller;
			word-wrap: break-word;
		}
		.heading {
			background-color: lightgray;
		}
	</style>
</head>
<body>
	<form id="form1" runat="server">
		<div style="max-width: 7.5in">
			<asp:FormView ID="FormView1" runat="server" DataSourceID="ObjectDataSource1">
				<ItemTemplate>
					<table>
						<tr class="heading">
							<th>Title</th>
							<th>Exploitability</th>
							<th>Detection Source</th>
							<th>Major Hash</th>
							<th>Minor Hash</th>
							<th>Iteration</th>
							<th>Is Reproduction</th>
						</tr>
						<tr>
							<td>
								<asp:Label ID="Label2" runat="server" Text='<%# Bind("Title") %>' />
							</td>
							<td>
								<asp:Label ID="Label1" runat="server" Text='<%# Bind("Exploitability") %>' />
							</td>
							<td>
								<asp:Label ID="Label3" runat="server" Text='<%# Bind("DetectionSource") %>' />
							</td>
							<td>
								<asp:Label ID="Label4" runat="server" Text='<%# Bind("MajorHash") %>' />
							</td>
							<td>
								<asp:Label ID="Label5" runat="server" Text='<%# Bind("MinorHash") %>' />
							</td>
							<td>
								<asp:Label ID="Label6" runat="server" Text='<%# Bind("Iteration") %>' />
							</td>
							<td>
								<asp:Label ID="Label7" runat="server" Text='<%# Bind("IsReproduction") %>' />
							</td>
						</tr>
						<tr class="heading">
							<th colspan="7">
								Description
							</th>
						</tr>
						<tr>
							<td colspan="7">
								<asp:Label ID="description" CssClass="mono" runat="server" Text='<%# Bind("Description") %>' />
							</td>
						</tr>
						<tr class="heading">
							<th colspan="7">
								Generated Files
							</th>
						</tr>
						<tr>
							<td colspan="7">
								<asp:Repeater ID="generatedFiles" runat="server" DataSource='<%# DataBinder.Eval(Container.DataItem, "GeneratedFileViewModels")%>'>
									<HeaderTemplate>
										<table>
									</HeaderTemplate>
									<ItemTemplate>
										<tr>
											<td>
												<asp:HyperLink ID="HyperLink1" runat="server" NavigateUrl='<%# "~/GetJobOutput.aspx?file=" + Eval("GridFsLocation") %>'><%#Eval("Name") %></asp:HyperLink>
											</td>
										</tr>
										<tr>
											<td style="display: none">
												<p>
													<asp:Label ID="Label8" runat="server" Text='<%#Eval("FullText") %>' />
													</p>
											</td>
										</tr>
										<tr>
											<td>
												<hr />
											</td>
										</tr>
									</ItemTemplate>
									<FooterTemplate>
										</table>
									</FooterTemplate>
					</asp:Repeater>
							</td>
						</tr>
					</table>
				</ItemTemplate>

			</asp:FormView>
			<asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetFaultData" TypeName="PeachFarmMonitor.FaultData">
				<SelectParameters>
					<asp:QueryStringParameter Name="faultid" QueryStringField="faultid" Type="String" />
				</SelectParameters>
			</asp:ObjectDataSource>
		</div>
	</form>

</body>
</html>

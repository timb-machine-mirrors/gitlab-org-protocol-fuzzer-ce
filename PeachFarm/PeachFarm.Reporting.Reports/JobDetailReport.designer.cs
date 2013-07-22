namespace PeachFarm.Reporting.Reports
{
	partial class JobDetailReport
	{
		#region Component Designer generated code
		/// <summary>
		/// Required method for telerik Reporting designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Telerik.Reporting.TableGroup tableGroup1 = new Telerik.Reporting.TableGroup();
			Telerik.Reporting.TableGroup tableGroup2 = new Telerik.Reporting.TableGroup();
			Telerik.Reporting.ReportParameter reportParameter1 = new Telerik.Reporting.ReportParameter();
			Telerik.Reporting.ReportParameter reportParameter2 = new Telerik.Reporting.ReportParameter();
			Telerik.Reporting.Drawing.StyleRule styleRule1 = new Telerik.Reporting.Drawing.StyleRule();
			Telerik.Reporting.Drawing.StyleRule styleRule2 = new Telerik.Reporting.Drawing.StyleRule();
			Telerik.Reporting.Drawing.StyleRule styleRule3 = new Telerik.Reporting.Drawing.StyleRule();
			Telerik.Reporting.Drawing.StyleRule styleRule4 = new Telerik.Reporting.Drawing.StyleRule();
			this.objectDataSource1 = new Telerik.Reporting.ObjectDataSource();
			this.pageHeader = new Telerik.Reporting.PageHeaderSection();
			this.reportNameTextBox = new Telerik.Reporting.TextBox();
			this.pageFooter = new Telerik.Reporting.PageFooterSection();
			this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
			this.titleTextBox = new Telerik.Reporting.TextBox();
			this.jobIDCaptionTextBox = new Telerik.Reporting.TextBox();
			this.jobIDDataTextBox = new Telerik.Reporting.TextBox();
			this.reportFooter = new Telerik.Reporting.ReportFooterSection();
			this.detail = new Telerik.Reporting.DetailSection();
			this.textBox1 = new Telerik.Reporting.TextBox();
			this.textBox2 = new Telerik.Reporting.TextBox();
			this.textBox3 = new Telerik.Reporting.TextBox();
			this.textBox4 = new Telerik.Reporting.TextBox();
			this.table1 = new Telerik.Reporting.Table();
			this.textBox7 = new Telerik.Reporting.TextBox();
			this.textBox8 = new Telerik.Reporting.TextBox();
			((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
			// 
			// objectDataSource1
			// 
			this.objectDataSource1.DataMember = "GetJobDetailReport";
			this.objectDataSource1.DataSource = typeof(PeachFarm.Reporting.Reports.ReportData);
			this.objectDataSource1.Name = "objectDataSource1";
			this.objectDataSource1.Parameters.AddRange(new Telerik.Reporting.ObjectDataSourceParameter[] {
            new Telerik.Reporting.ObjectDataSourceParameter("jobID", typeof(string), "=Parameters.jobID.Value"),
            new Telerik.Reporting.ObjectDataSourceParameter("connectionString", typeof(string), "=Parameters.connectionString.Value")});
			// 
			// pageHeader
			// 
			this.pageHeader.Height = Telerik.Reporting.Drawing.Unit.Inch(0D);
			this.pageHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.reportNameTextBox});
			this.pageHeader.Name = "pageHeader";
			// 
			// reportNameTextBox
			// 
			this.reportNameTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.02083333395421505D), Telerik.Reporting.Drawing.Unit.Inch(0.02083333395421505D));
			this.reportNameTextBox.Name = "reportNameTextBox";
			this.reportNameTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(6.4166665077209473D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.reportNameTextBox.StyleName = "PageInfo";
			this.reportNameTextBox.Value = "Job Detail Report";
			// 
			// pageFooter
			// 
			this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Inch(0D);
			this.pageFooter.Name = "pageFooter";
			// 
			// reportHeader
			// 
			this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Inch(1.2290682792663574D);
			this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.titleTextBox,
            this.jobIDCaptionTextBox,
            this.jobIDDataTextBox});
			this.reportHeader.Name = "reportHeader";
			// 
			// titleTextBox
			// 
			this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0D), Telerik.Reporting.Drawing.Unit.Inch(0D));
			this.titleTextBox.Name = "titleTextBox";
			this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(6.4583334922790527D), Telerik.Reporting.Drawing.Unit.Inch(0.787401556968689D));
			this.titleTextBox.StyleName = "Title";
			this.titleTextBox.Value = "Job Detail Report";
			// 
			// jobIDCaptionTextBox
			// 
			this.jobIDCaptionTextBox.CanGrow = true;
			this.jobIDCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.02083333395421505D), Telerik.Reporting.Drawing.Unit.Inch(0.80823493003845215D));
			this.jobIDCaptionTextBox.Name = "jobIDCaptionTextBox";
			this.jobIDCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(3.1979167461395264D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.jobIDCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.jobIDCaptionTextBox.StyleName = "Caption";
			this.jobIDCaptionTextBox.Value = "Job ID:";
			// 
			// jobIDDataTextBox
			// 
			this.jobIDDataTextBox.CanGrow = true;
			this.jobIDDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.2395832538604736D), Telerik.Reporting.Drawing.Unit.Inch(0.80823493003845215D));
			this.jobIDDataTextBox.Name = "jobIDDataTextBox";
			this.jobIDDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(3.1979167461395264D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.jobIDDataTextBox.StyleName = "Data";
			this.jobIDDataTextBox.Value = "=Fields.JobID";
			// 
			// reportFooter
			// 
			this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Inch(0D);
			this.reportFooter.Name = "reportFooter";
			// 
			// detail
			// 
			this.detail.Height = Telerik.Reporting.Drawing.Unit.Inch(1.470931887626648D);
			this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox1,
            this.textBox2,
            this.textBox3,
            this.textBox4,
            this.table1});
			this.detail.Name = "detail";
			// 
			// textBox1
			// 
			this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0D), Telerik.Reporting.Drawing.Unit.Inch(0D));
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D), Telerik.Reporting.Drawing.Unit.Inch(0.27093172073364258D));
			this.textBox1.Style.Font.Bold = true;
			this.textBox1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(12D);
			this.textBox1.Value = "=Fields.Iteration";
			// 
			// textBox2
			// 
			this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D), Telerik.Reporting.Drawing.Unit.Inch(0D));
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(4.2999210357666016D), Telerik.Reporting.Drawing.Unit.Inch(0.27093172073364258D));
			this.textBox2.Value = "=Fields.Title";
			// 
			// textBox3
			// 
			this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(4.70007848739624D), Telerik.Reporting.Drawing.Unit.Inch(0D));
			this.textBox3.Name = "textBox3";
			this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1.7582544088363648D), Telerik.Reporting.Drawing.Unit.Inch(0.27093172073364258D));
			this.textBox3.Value = "=Fields.FolderName";
			// 
			// textBox4
			// 
			this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.02083333395421505D), Telerik.Reporting.Drawing.Unit.Inch(0.27101054787635803D));
			this.textBox4.Name = "textBox4";
			this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(6.4374604225158691D), Telerik.Reporting.Drawing.Unit.Inch(0.3999999463558197D));
			this.textBox4.Value = "=Fields.Description";
			// 
			// table1
			// 
			this.table1.Bindings.Add(new Telerik.Reporting.Binding("DataSource", "=Fields.GeneratedFiles"));
			this.table1.Body.Columns.Add(new Telerik.Reporting.TableBodyColumn(Telerik.Reporting.Drawing.Unit.Inch(6.458254337310791D)));
			this.table1.Body.Rows.Add(new Telerik.Reporting.TableBodyRow(Telerik.Reporting.Drawing.Unit.Inch(0.25D)));
			this.table1.Body.SetCellContent(0, 0, this.textBox8);
			tableGroup1.Name = "tableGroup1";
			tableGroup1.ReportItem = this.textBox7;
			this.table1.ColumnGroups.Add(tableGroup1);
			this.table1.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox7,
            this.textBox8});
			this.table1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D), Telerik.Reporting.Drawing.Unit.Inch(0.67108935117721558D));
			this.table1.Name = "table1";
			tableGroup2.Groupings.Add(new Telerik.Reporting.Grouping(null));
			tableGroup2.Name = "detailTableGroup";
			this.table1.RowGroups.Add(tableGroup2);
			this.table1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(6.458254337310791D), Telerik.Reporting.Drawing.Unit.Inch(0.5D));
			// 
			// textBox7
			// 
			this.textBox7.Name = "textBox7";
			this.textBox7.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(6.458254337310791D), Telerik.Reporting.Drawing.Unit.Inch(0.25D));
			this.textBox7.Value = "Generated Files";
			// 
			// textBox8
			// 
			this.textBox8.Name = "textBox8";
			this.textBox8.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(6.458254337310791D), Telerik.Reporting.Drawing.Unit.Inch(0.25D));
			this.textBox8.Value = "=Fields.GridFsLocation";
			// 
			// JobDetailReport
			// 
			this.DataSource = this.objectDataSource1;
			this.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.pageHeader,
            this.pageFooter,
            this.reportHeader,
            this.reportFooter,
            this.detail});
			this.Name = "JobDetailReport";
			this.PageSettings.Margins = new Telerik.Reporting.Drawing.MarginsU(Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(1D));
			this.PageSettings.PaperKind = System.Drawing.Printing.PaperKind.Letter;
			reportParameter1.Name = "jobID";
			reportParameter1.Text = "jobID";
			reportParameter2.Name = "connectionString";
			reportParameter2.Text = "connectionString";
			this.ReportParameters.Add(reportParameter1);
			this.ReportParameters.Add(reportParameter2);
			this.Style.BackgroundColor = System.Drawing.Color.White;
			styleRule1.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Title")});
			styleRule1.Style.Color = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
			styleRule1.Style.Font.Name = "Tahoma";
			styleRule1.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(18D);
			styleRule2.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Caption")});
			styleRule2.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(58)))), ((int)(((byte)(112)))));
			styleRule2.Style.Color = System.Drawing.Color.White;
			styleRule2.Style.Font.Name = "Tahoma";
			styleRule2.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(10D);
			styleRule2.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
			styleRule3.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("Data")});
			styleRule3.Style.Color = System.Drawing.Color.Black;
			styleRule3.Style.Font.Name = "Tahoma";
			styleRule3.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(9D);
			styleRule3.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
			styleRule4.Selectors.AddRange(new Telerik.Reporting.Drawing.ISelector[] {
            new Telerik.Reporting.Drawing.StyleSelector("PageInfo")});
			styleRule4.Style.Color = System.Drawing.Color.Black;
			styleRule4.Style.Font.Name = "Tahoma";
			styleRule4.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(8D);
			styleRule4.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Middle;
			this.StyleSheet.AddRange(new Telerik.Reporting.Drawing.StyleRule[] {
            styleRule1,
            styleRule2,
            styleRule3,
            styleRule4});
			this.Width = Telerik.Reporting.Drawing.Unit.Inch(6.4583334922790527D);
			((System.ComponentModel.ISupportInitialize)(this)).EndInit();

		}
		#endregion

		private Telerik.Reporting.ObjectDataSource objectDataSource1;
		private Telerik.Reporting.PageHeaderSection pageHeader;
		private Telerik.Reporting.TextBox reportNameTextBox;
		private Telerik.Reporting.PageFooterSection pageFooter;
		private Telerik.Reporting.ReportHeaderSection reportHeader;
		private Telerik.Reporting.TextBox titleTextBox;
		private Telerik.Reporting.TextBox jobIDCaptionTextBox;
		private Telerik.Reporting.TextBox jobIDDataTextBox;
		private Telerik.Reporting.ReportFooterSection reportFooter;
		private Telerik.Reporting.DetailSection detail;
		private Telerik.Reporting.TextBox textBox1;
		private Telerik.Reporting.TextBox textBox2;
		private Telerik.Reporting.TextBox textBox3;
		private Telerik.Reporting.TextBox textBox4;
		private Telerik.Reporting.Table table1;
		private Telerik.Reporting.TextBox textBox8;
		private Telerik.Reporting.TextBox textBox7;

	}
}
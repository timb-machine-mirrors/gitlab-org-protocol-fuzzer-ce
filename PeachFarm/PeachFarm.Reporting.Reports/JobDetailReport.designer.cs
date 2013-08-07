using System.Globalization;

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
			Telerik.Reporting.NavigateToUrlAction navigateToUrlAction1 = new Telerik.Reporting.NavigateToUrlAction();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobDetailReport));
			Telerik.Reporting.TableGroup tableGroup3 = new Telerik.Reporting.TableGroup();
			Telerik.Reporting.TableGroup tableGroup4 = new Telerik.Reporting.TableGroup();
			Telerik.Reporting.TableGroup tableGroup1 = new Telerik.Reporting.TableGroup();
			Telerik.Reporting.TableGroup tableGroup2 = new Telerik.Reporting.TableGroup();
			Telerik.Reporting.NavigateToUrlAction navigateToUrlAction2 = new Telerik.Reporting.NavigateToUrlAction();
			Telerik.Reporting.ReportParameter reportParameter1 = new Telerik.Reporting.ReportParameter();
			Telerik.Reporting.ReportParameter reportParameter2 = new Telerik.Reporting.ReportParameter();
			Telerik.Reporting.ReportParameter reportParameter3 = new Telerik.Reporting.ReportParameter();
			this.peachFarmMongo = new Telerik.Reporting.ObjectDataSource();
			this.pageHeader = new Telerik.Reporting.PageHeaderSection();
			this.pageFooter = new Telerik.Reporting.PageFooterSection();
			this.reportHeader = new Telerik.Reporting.ReportHeaderSection();
			this.jobOutputLink = new Telerik.Reporting.TextBox();
			this.startDateDataTextBox = new Telerik.Reporting.TextBox();
			this.startDateCaptionTextBox = new Telerik.Reporting.TextBox();
			this.userNameDataTextBox = new Telerik.Reporting.TextBox();
			this.userNameCaptionTextBox = new Telerik.Reporting.TextBox();
			this.pitFileNameDataTextBox = new Telerik.Reporting.TextBox();
			this.pitFileNameCaptionTextBox = new Telerik.Reporting.TextBox();
			this.jobIDDataTextBox = new Telerik.Reporting.TextBox();
			this.jobIDCaptionTextBox = new Telerik.Reporting.TextBox();
			this.titleTextBox = new Telerik.Reporting.TextBox();
			this.pictureBox1 = new Telerik.Reporting.PictureBox();
			this.reportFooter = new Telerik.Reporting.ReportFooterSection();
			this.detail = new Telerik.Reporting.DetailSection();
			this.faultsList = new Telerik.Reporting.List();
			this.panel2 = new Telerik.Reporting.Panel();
			this.textBox12 = new Telerik.Reporting.TextBox();
			this.textBox13 = new Telerik.Reporting.TextBox();
			this.textBox14 = new Telerik.Reporting.TextBox();
			this.textBox15 = new Telerik.Reporting.TextBox();
			this.textBox16 = new Telerik.Reporting.TextBox();
			this.textBox17 = new Telerik.Reporting.TextBox();
			this.textBox20 = new Telerik.Reporting.TextBox();
			this.textBox21 = new Telerik.Reporting.TextBox();
			this.textBox18 = new Telerik.Reporting.TextBox();
			this.textBox19 = new Telerik.Reporting.TextBox();
			this.textBox23 = new Telerik.Reporting.TextBox();
			this.textBox9 = new Telerik.Reporting.TextBox();
			this.textBox11 = new Telerik.Reporting.TextBox();
			this.textBox37 = new Telerik.Reporting.TextBox();
			this.generatedFilesDataTable = new Telerik.Reporting.Table();
			this.textBox41 = new Telerik.Reporting.TextBox();
			this.textBox2 = new Telerik.Reporting.TextBox();
			this.textBox1 = new Telerik.Reporting.TextBox();
			this.textBox3 = new Telerik.Reporting.TextBox();
			this.textBox4 = new Telerik.Reporting.TextBox();
			((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
			// 
			// peachFarmMongo
			// 
			this.peachFarmMongo.DataMember = "GetJobDetailReport";
			this.peachFarmMongo.DataSource = typeof(PeachFarm.Reporting.Reports.ReportData);
			this.peachFarmMongo.Name = "peachFarmMongo";
			this.peachFarmMongo.Parameters.AddRange(new Telerik.Reporting.ObjectDataSourceParameter[] {
            new Telerik.Reporting.ObjectDataSourceParameter("jobID", typeof(string), "=Parameters.jobID.Value"),
            new Telerik.Reporting.ObjectDataSourceParameter("connectionString", typeof(string), "=Parameters.connectionString.Value")});
			// 
			// pageHeader
			// 
			this.pageHeader.Height = Telerik.Reporting.Drawing.Unit.Inch(0D);
			this.pageHeader.Name = "pageHeader";
			// 
			// pageFooter
			// 
			this.pageFooter.Height = Telerik.Reporting.Drawing.Unit.Inch(0D);
			this.pageFooter.Name = "pageFooter";
			// 
			// reportHeader
			// 
			this.reportHeader.Height = Telerik.Reporting.Drawing.Unit.Inch(2.5999999046325684D);
			this.reportHeader.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.jobOutputLink,
            this.startDateDataTextBox,
            this.startDateCaptionTextBox,
            this.userNameDataTextBox,
            this.userNameCaptionTextBox,
            this.pitFileNameDataTextBox,
            this.pitFileNameCaptionTextBox,
            this.jobIDDataTextBox,
            this.jobIDCaptionTextBox,
            this.titleTextBox,
            this.pictureBox1});
			this.reportHeader.Name = "reportHeader";
			// 
			// jobOutputLink
			// 
			navigateToUrlAction1.Url = "= Parameters.hostURL.Value + \"GetJobOutput.aspx?jobid=\" + Fields.JobID";
			this.jobOutputLink.Action = navigateToUrlAction1;
			this.jobOutputLink.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D), Telerik.Reporting.Drawing.Unit.Inch(1.3542060852050781D));
			this.jobOutputLink.Name = "jobOutputLink";
			this.jobOutputLink.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.49992036819458D), Telerik.Reporting.Drawing.Unit.Inch(0.24158795177936554D));
			this.jobOutputLink.Style.Color = System.Drawing.Color.Blue;
			this.jobOutputLink.Style.Font.Underline = true;
			this.jobOutputLink.StyleName = "Data";
			this.jobOutputLink.Value = "Download Full Job Output";
			// 
			// startDateDataTextBox
			// 
			this.startDateDataTextBox.CanGrow = true;
			this.startDateDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(4.6979560852050781D), Telerik.Reporting.Drawing.Unit.Inch(2.1042060852050781D));
			this.startDateDataTextBox.Name = "startDateDataTextBox";
			this.startDateDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.8000786304473877D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.startDateDataTextBox.StyleName = "Data";
			this.startDateDataTextBox.Value = "=Fields.StartDate";
			// 
			// startDateCaptionTextBox
			// 
			this.startDateCaptionTextBox.CanGrow = true;
			this.startDateCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.5937893390655518D), Telerik.Reporting.Drawing.Unit.Inch(2.0937893390655518D));
			this.startDateCaptionTextBox.Name = "startDateCaptionTextBox";
			this.startDateCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99992114305496216D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.startDateCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.startDateCaptionTextBox.StyleName = "Caption";
			this.startDateCaptionTextBox.Value = "Start Date:";
			// 
			// userNameDataTextBox
			// 
			this.userNameDataTextBox.CanGrow = true;
			this.userNameDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(1.0937894582748413D), Telerik.Reporting.Drawing.Unit.Inch(2.0937893390655518D));
			this.userNameDataTextBox.Name = "userNameDataTextBox";
			this.userNameDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.39996075630188D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.userNameDataTextBox.StyleName = "Data";
			this.userNameDataTextBox.Value = "=Fields.UserName";
			// 
			// userNameCaptionTextBox
			// 
			this.userNameCaptionTextBox.CanGrow = true;
			this.userNameCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D), Telerik.Reporting.Drawing.Unit.Inch(2.0937893390655518D));
			this.userNameCaptionTextBox.Name = "userNameCaptionTextBox";
			this.userNameCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.userNameCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.userNameCaptionTextBox.StyleName = "Caption";
			this.userNameCaptionTextBox.Value = "User Name:";
			// 
			// pitFileNameDataTextBox
			// 
			this.pitFileNameDataTextBox.CanGrow = true;
			this.pitFileNameDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(4.6979560852050781D), Telerik.Reporting.Drawing.Unit.Inch(1.6979560852050781D));
			this.pitFileNameDataTextBox.Name = "pitFileNameDataTextBox";
			this.pitFileNameDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.8000786304473877D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.pitFileNameDataTextBox.StyleName = "Data";
			this.pitFileNameDataTextBox.Value = "=Fields.Pit";
			// 
			// pitFileNameCaptionTextBox
			// 
			this.pitFileNameCaptionTextBox.CanGrow = true;
			this.pitFileNameCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.5937893390655518D), Telerik.Reporting.Drawing.Unit.Inch(1.6979560852050781D));
			this.pitFileNameCaptionTextBox.Name = "pitFileNameCaptionTextBox";
			this.pitFileNameCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99992114305496216D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.pitFileNameCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.pitFileNameCaptionTextBox.StyleName = "Caption";
			this.pitFileNameCaptionTextBox.Value = "Pit File Name:";
			// 
			// jobIDDataTextBox
			// 
			this.jobIDDataTextBox.CanGrow = true;
			this.jobIDDataTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(1.0937894582748413D), Telerik.Reporting.Drawing.Unit.Inch(1.6979560852050781D));
			this.jobIDDataTextBox.Name = "jobIDDataTextBox";
			this.jobIDDataTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.39996075630188D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.jobIDDataTextBox.StyleName = "Data";
			this.jobIDDataTextBox.Value = "=Fields.JobID";
			// 
			// jobIDCaptionTextBox
			// 
			this.jobIDCaptionTextBox.CanGrow = true;
			this.jobIDCaptionTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D), Telerik.Reporting.Drawing.Unit.Inch(1.6979560852050781D));
			this.jobIDCaptionTextBox.Name = "jobIDCaptionTextBox";
			this.jobIDCaptionTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.jobIDCaptionTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.jobIDCaptionTextBox.StyleName = "Caption";
			this.jobIDCaptionTextBox.Value = "Job ID:";
			// 
			// titleTextBox
			// 
			this.titleTextBox.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D), Telerik.Reporting.Drawing.Unit.Inch(1.0000394582748413D));
			this.titleTextBox.Name = "titleTextBox";
			this.titleTextBox.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.4999604225158691D), Telerik.Reporting.Drawing.Unit.Inch(0.35833334922790527D));
			this.titleTextBox.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Center;
			this.titleTextBox.StyleName = "Title";
			this.titleTextBox.Value = "= \"Detail Report for Job:\" + Fields.JobID";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0D), Telerik.Reporting.Drawing.Unit.Inch(0D));
			this.pictureBox1.MimeType = "image/jpeg";
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.5D), Telerik.Reporting.Drawing.Unit.Inch(1D));
			this.pictureBox1.Sizing = Telerik.Reporting.Drawing.ImageSizeMode.ScaleProportional;
			//this.pictureBox1.Value = ((object)(resources.GetObject("pictureBox1.Value")));
			this.pictureBox1.Value = ReportData.GetEmbeddedImage("dejavulogo.jpg");
			// 
			// reportFooter
			// 
			this.reportFooter.Height = Telerik.Reporting.Drawing.Unit.Inch(0D);
			this.reportFooter.Name = "reportFooter";
			// 
			// detail
			// 
			this.detail.Height = Telerik.Reporting.Drawing.Unit.Inch(3.1000003814697266D);
			this.detail.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.faultsList});
			this.detail.KeepTogether = false;
			this.detail.Name = "detail";
			// 
			// faultsList
			// 
			this.faultsList.Bindings.Add(new Telerik.Reporting.Binding("DataSource", "=ReportItem.DataObject.Faults"));
			this.faultsList.Body.Columns.Add(new Telerik.Reporting.TableBodyColumn(Telerik.Reporting.Drawing.Unit.Inch(7.4999604225158691D)));
			this.faultsList.Body.Rows.Add(new Telerik.Reporting.TableBodyRow(Telerik.Reporting.Drawing.Unit.Inch(3.0999610424041748D)));
			this.faultsList.Body.SetCellContent(0, 0, this.panel2);
			tableGroup3.Name = "ColumnGroup";
			this.faultsList.ColumnGroups.Add(tableGroup3);
			this.faultsList.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.panel2});
			this.faultsList.KeepTogether = false;
			this.faultsList.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.faultsList.Name = "faultsList";
			tableGroup4.Groupings.Add(new Telerik.Reporting.Grouping(null));
			tableGroup4.Name = "DetailGroup";
			this.faultsList.RowGroups.Add(tableGroup4);
			this.faultsList.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.4999604225158691D), Telerik.Reporting.Drawing.Unit.Inch(3.0999610424041748D));
			// 
			// panel2
			// 
			this.panel2.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox12,
            this.textBox13,
            this.textBox14,
            this.textBox15,
            this.textBox16,
            this.textBox17,
            this.textBox20,
            this.textBox21,
            this.textBox18,
            this.textBox19,
            this.textBox23,
            this.textBox9,
            this.textBox11,
            this.textBox37,
            this.generatedFilesDataTable,
            this.textBox2,
            this.textBox1,
            this.textBox3,
            this.textBox4});
			this.panel2.KeepTogether = false;
			this.panel2.Name = "panel2";
			this.panel2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.4999604225158691D), Telerik.Reporting.Drawing.Unit.Inch(3.0999610424041748D));
			// 
			// textBox12
			// 
			this.textBox12.CanGrow = true;
			this.textBox12.KeepTogether = false;
			this.textBox12.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.20003898441791534D), Telerik.Reporting.Drawing.Unit.Inch(0.40011820197105408D));
			this.textBox12.Name = "textBox12";
			this.textBox12.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99992114305496216D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox12.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox12.StyleName = "Caption";
			this.textBox12.Value = "Exploitability:";
			// 
			// textBox13
			// 
			this.textBox13.CanGrow = true;
			this.textBox13.KeepTogether = false;
			this.textBox13.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(1.2999597787857056D), Telerik.Reporting.Drawing.Unit.Inch(0.40011820197105408D));
			this.textBox13.Name = "textBox13";
			this.textBox13.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.2000007629394531D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox13.StyleName = "Data";
			this.textBox13.Value = "=Fields.Exploitability";
			// 
			// textBox14
			// 
			this.textBox14.CanGrow = true;
			this.textBox14.KeepTogether = false;
			this.textBox14.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.20003898441791534D), Telerik.Reporting.Drawing.Unit.Inch(0.80015754699707031D));
			this.textBox14.Name = "textBox14";
			this.textBox14.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(0.38533791899681091D));
			this.textBox14.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox14.StyleName = "Caption";
			this.textBox14.Value = "Source:";
			// 
			// textBox15
			// 
			this.textBox15.CanGrow = true;
			this.textBox15.KeepTogether = false;
			this.textBox15.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(1.2999597787857056D), Telerik.Reporting.Drawing.Unit.Inch(0.80015754699707031D));
			this.textBox15.Name = "textBox15";
			this.textBox15.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.2000007629394531D), Telerik.Reporting.Drawing.Unit.Inch(0.38533791899681091D));
			this.textBox15.StyleName = "Data";
			this.textBox15.Value = "=Fields.DetectionSource";
			// 
			// textBox16
			// 
			this.textBox16.CanGrow = true;
			this.textBox16.KeepTogether = false;
			this.textBox16.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.6001179218292236D), Telerik.Reporting.Drawing.Unit.Inch(0.40436363220214844D));
			this.textBox16.Name = "textBox16";
			this.textBox16.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99992114305496216D), Telerik.Reporting.Drawing.Unit.Inch(0.38533791899681091D));
			this.textBox16.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox16.StyleName = "Caption";
			this.textBox16.Value = "Major Hash:";
			// 
			// textBox17
			// 
			this.textBox17.CanGrow = true;
			this.textBox17.KeepTogether = false;
			this.textBox17.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(4.7104568481445312D), Telerik.Reporting.Drawing.Unit.Inch(0.40436363220214844D));
			this.textBox17.Name = "textBox17";
			this.textBox17.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.789503812789917D), Telerik.Reporting.Drawing.Unit.Inch(0.38533791899681091D));
			this.textBox17.StyleName = "Data";
			this.textBox17.Value = "=Fields.MajorHash";
			// 
			// textBox20
			// 
			this.textBox20.CanGrow = true;
			this.textBox20.KeepTogether = false;
			this.textBox20.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(4.70428466796875D), Telerik.Reporting.Drawing.Unit.Inch(0.80015754699707031D));
			this.textBox20.Name = "textBox20";
			this.textBox20.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(2.7956759929656982D), Telerik.Reporting.Drawing.Unit.Inch(0.38533791899681091D));
			this.textBox20.StyleName = "Data";
			this.textBox20.Value = "=Fields.MinorHash";
			// 
			// textBox21
			// 
			this.textBox21.CanGrow = true;
			this.textBox21.KeepTogether = false;
			this.textBox21.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(3.6001179218292236D), Telerik.Reporting.Drawing.Unit.Inch(0.78978031873703D));
			this.textBox21.Name = "textBox21";
			this.textBox21.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99992114305496216D), Telerik.Reporting.Drawing.Unit.Inch(0.3957151472568512D));
			this.textBox21.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox21.StyleName = "Caption";
			this.textBox21.Value = "Minor Hash:";
			// 
			// textBox18
			// 
			this.textBox18.CanGrow = true;
			this.textBox18.KeepTogether = false;
			this.textBox18.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.19992192089557648D), Telerik.Reporting.Drawing.Unit.Inch(1.8999605178833008D));
			this.textBox18.Name = "textBox18";
			this.textBox18.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.2957940101623535D), Telerik.Reporting.Drawing.Unit.Inch(0.22461645305156708D));
			this.textBox18.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox18.Style.Color = System.Drawing.Color.White;
			this.textBox18.Style.Font.Bold = true;
			this.textBox18.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
			this.textBox18.StyleName = "Caption";
			this.textBox18.Value = "Description";
			// 
			// textBox19
			// 
			this.textBox19.CanGrow = true;
			this.textBox19.KeepTogether = false;
			this.textBox19.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.19992192089557648D), Telerik.Reporting.Drawing.Unit.Inch(2.1246564388275146D));
			this.textBox19.Name = "textBox19";
			this.textBox19.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.2957940101623535D), Telerik.Reporting.Drawing.Unit.Inch(0.9041561484336853D));
			this.textBox19.Style.Font.Name = "Consolas";
			this.textBox19.Style.Font.Size = Telerik.Reporting.Drawing.Unit.Point(8D);
			this.textBox19.Style.VerticalAlign = Telerik.Reporting.Drawing.VerticalAlign.Top;
			this.textBox19.StyleName = "Data";
			this.textBox19.Value = "=Fields.Description";
			// 
			// textBox23
			// 
			this.textBox23.CanGrow = true;
			this.textBox23.KeepTogether = false;
			this.textBox23.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(1.2000389099121094D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox23.Name = "textBox23";
			this.textBox23.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.083254493772983551D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox23.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox23.Style.Color = System.Drawing.Color.White;
			this.textBox23.Style.Font.Bold = true;
			this.textBox23.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox23.StyleName = "Caption";
			this.textBox23.Value = "";
			// 
			// textBox9
			// 
			this.textBox9.CanGrow = true;
			this.textBox9.KeepTogether = false;
			this.textBox9.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.20003898441791534D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox9.Name = "textBox9";
			this.textBox9.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99606949090957642D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox9.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox9.Style.Color = System.Drawing.Color.White;
			this.textBox9.Style.Font.Bold = true;
			this.textBox9.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox9.StyleName = "Caption";
			this.textBox9.Value = "Fault:";
			// 
			// textBox11
			// 
			this.textBox11.CanGrow = true;
			this.textBox11.DocumentMapText = "";
			this.textBox11.KeepTogether = false;
			this.textBox11.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(1.2833722829818726D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox11.Name = "textBox11";
			this.textBox11.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(4.0536942481994629D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox11.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox11.Style.Color = System.Drawing.Color.White;
			this.textBox11.Style.Font.Bold = true;
			this.textBox11.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
			this.textBox11.StyleName = "Caption";
			this.textBox11.Value = "=Fields.Title";
			// 
			// textBox37
			// 
			this.textBox37.CanGrow = true;
			this.textBox37.KeepTogether = false;
			this.textBox37.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.20003890991210938D), Telerik.Reporting.Drawing.Unit.Inch(1.299960732460022D));
			this.textBox37.Name = "textBox37";
			this.textBox37.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.2956771850585938D), Telerik.Reporting.Drawing.Unit.Inch(0.22461645305156708D));
			this.textBox37.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox37.Style.Color = System.Drawing.Color.White;
			this.textBox37.Style.Font.Bold = true;
			this.textBox37.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
			this.textBox37.StyleName = "Caption";
			this.textBox37.Value = "Generated Files";
			// 
			// generatedFilesDataTable
			// 
			this.generatedFilesDataTable.Bindings.Add(new Telerik.Reporting.Binding("DataSource", "=ReportItem.DataObject.GeneratedFiles"));
			this.generatedFilesDataTable.Body.Columns.Add(new Telerik.Reporting.TableBodyColumn(Telerik.Reporting.Drawing.Unit.Inch(7.2956376075744629D)));
			this.generatedFilesDataTable.Body.Rows.Add(new Telerik.Reporting.TableBodyRow(Telerik.Reporting.Drawing.Unit.Inch(0.19999949634075165D)));
			this.generatedFilesDataTable.Body.SetCellContent(0, 0, this.textBox41);
			this.generatedFilesDataTable.ColumnGroups.Add(tableGroup1);
			this.generatedFilesDataTable.Items.AddRange(new Telerik.Reporting.ReportItemBase[] {
            this.textBox41});
			this.generatedFilesDataTable.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(0.20003890991210938D), Telerik.Reporting.Drawing.Unit.Inch(1.5246553421020508D));
			this.generatedFilesDataTable.Name = "generatedFilesDataTable";
			tableGroup2.Groupings.Add(new Telerik.Reporting.Grouping(null));
			tableGroup2.Name = "DetailGroup";
			this.generatedFilesDataTable.RowGroups.Add(tableGroup2);
			this.generatedFilesDataTable.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.2956376075744629D), Telerik.Reporting.Drawing.Unit.Inch(0.19999949634075165D));
			// 
			// textBox41
			// 
			navigateToUrlAction2.Url = "= Parameters.hostURL.Value + \"GetJobOutput.aspx?file=\" + Replace(Fields.GridFSLoc" +
    "ation,\"\\\",\"/\")";
			this.textBox41.Action = navigateToUrlAction2;
			this.textBox41.KeepTogether = false;
			this.textBox41.Name = "textBox41";
			this.textBox41.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(7.2956376075744629D), Telerik.Reporting.Drawing.Unit.Inch(0.19999949634075165D));
			this.textBox41.Style.Color = System.Drawing.Color.Blue;
			this.textBox41.Style.Font.Underline = true;
			this.textBox41.Value = "=Fields.Name";
			// 
			// textBox2
			// 
			this.textBox2.CanGrow = true;
			this.textBox2.KeepTogether = false;
			this.textBox2.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(5.4204788208007812D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.99606949090957642D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox2.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox2.Style.Color = System.Drawing.Color.White;
			this.textBox2.Style.Font.Bold = true;
			this.textBox2.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox2.StyleName = "Caption";
			this.textBox2.Value = "Iterations:";
			// 
			// textBox1
			// 
			this.textBox1.CanGrow = true;
			this.textBox1.DocumentMapText = "";
			this.textBox1.KeepTogether = false;
			this.textBox1.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(6.4999604225158691D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(1D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox1.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox1.Style.Color = System.Drawing.Color.White;
			this.textBox1.Style.Font.Bold = true;
			this.textBox1.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Left;
			this.textBox1.StyleName = "Caption";
			this.textBox1.Value = "=Fields.FaultCount";
			// 
			// textBox3
			// 
			this.textBox3.CanGrow = true;
			this.textBox3.KeepTogether = false;
			this.textBox3.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(6.4166274070739746D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox3.Name = "textBox3";
			this.textBox3.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.083254493772983551D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox3.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox3.Style.Color = System.Drawing.Color.White;
			this.textBox3.Style.Font.Bold = true;
			this.textBox3.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox3.StyleName = "Caption";
			this.textBox3.Value = "";
			// 
			// textBox4
			// 
			this.textBox4.CanGrow = true;
			this.textBox4.KeepTogether = false;
			this.textBox4.Location = new Telerik.Reporting.Drawing.PointU(Telerik.Reporting.Drawing.Unit.Inch(5.3371453285217285D), Telerik.Reporting.Drawing.Unit.Inch(3.9418537198798731E-05D));
			this.textBox4.Name = "textBox4";
			this.textBox4.Size = new Telerik.Reporting.Drawing.SizeU(Telerik.Reporting.Drawing.Unit.Inch(0.083254493772983551D), Telerik.Reporting.Drawing.Unit.Inch(0.40000000596046448D));
			this.textBox4.Style.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(179)))), ((int)(((byte)(94)))), ((int)(((byte)(6)))));
			this.textBox4.Style.Color = System.Drawing.Color.White;
			this.textBox4.Style.Font.Bold = true;
			this.textBox4.Style.TextAlign = Telerik.Reporting.Drawing.HorizontalAlign.Right;
			this.textBox4.StyleName = "Caption";
			this.textBox4.Value = "";
			// 
			// JobDetailReport
			// 
			this.DataSource = this.peachFarmMongo;
			this.ExternalStyleSheets.Add(new Telerik.Reporting.Drawing.ExternalStyleSheet("PeachFarm.Reporting.Reports.Resources.pfreportstyle.xml"));
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
			reportParameter1.Text = "Job ID";
			reportParameter1.Visible = true;
			reportParameter2.Name = "connectionString";
			reportParameter2.Visible = true;
			reportParameter3.Name = "hostURL";
			reportParameter3.Visible = true;
			this.ReportParameters.Add(reportParameter1);
			this.ReportParameters.Add(reportParameter2);
			this.ReportParameters.Add(reportParameter3);
			this.Style.BackgroundColor = System.Drawing.Color.White;
			this.Width = Telerik.Reporting.Drawing.Unit.Inch(7.5D);
			((System.ComponentModel.ISupportInitialize)(this)).EndInit();

		}
		#endregion

		private Telerik.Reporting.ObjectDataSource peachFarmMongo;
		private Telerik.Reporting.PageHeaderSection pageHeader;
		private Telerik.Reporting.PageFooterSection pageFooter;
		private Telerik.Reporting.ReportHeaderSection reportHeader;
		private Telerik.Reporting.ReportFooterSection reportFooter;
		private Telerik.Reporting.DetailSection detail;
		private Telerik.Reporting.List faultsList;
		private Telerik.Reporting.Panel panel2;
		private Telerik.Reporting.TextBox textBox12;
		private Telerik.Reporting.TextBox textBox13;
		private Telerik.Reporting.TextBox textBox14;
		private Telerik.Reporting.TextBox textBox15;
		private Telerik.Reporting.TextBox textBox16;
		private Telerik.Reporting.TextBox textBox17;
		private Telerik.Reporting.TextBox textBox20;
		private Telerik.Reporting.TextBox textBox21;
		private Telerik.Reporting.TextBox textBox18;
		private Telerik.Reporting.TextBox textBox19;
		private Telerik.Reporting.TextBox textBox23;
		private Telerik.Reporting.TextBox textBox9;
		private Telerik.Reporting.TextBox textBox11;
		private Telerik.Reporting.TextBox textBox37;
		private Telerik.Reporting.Table generatedFilesDataTable;
		private Telerik.Reporting.TextBox textBox41;
		private Telerik.Reporting.TextBox textBox2;
		private Telerik.Reporting.TextBox textBox1;
		private Telerik.Reporting.TextBox textBox3;
		private Telerik.Reporting.TextBox textBox4;
		private Telerik.Reporting.TextBox jobOutputLink;
		private Telerik.Reporting.TextBox startDateDataTextBox;
		private Telerik.Reporting.TextBox startDateCaptionTextBox;
		private Telerik.Reporting.TextBox userNameDataTextBox;
		private Telerik.Reporting.TextBox userNameCaptionTextBox;
		private Telerik.Reporting.TextBox pitFileNameDataTextBox;
		private Telerik.Reporting.TextBox pitFileNameCaptionTextBox;
		private Telerik.Reporting.TextBox jobIDDataTextBox;
		private Telerik.Reporting.TextBox jobIDCaptionTextBox;
		private Telerik.Reporting.TextBox titleTextBox;
		private Telerik.Reporting.PictureBox pictureBox1;

	}
}
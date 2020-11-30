﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VatTu.SimpleForm
{
    public partial class FormKho : Form
    {
        int position = 0;
        string maCN = "";

        public Stack<String> history_kho;

        // Undo Type
        String THEM_BTN = "_&them"; // Click btn thêm
        String XOA_BTN = "_&xoa"; // Click btn xóa
        String GHI_BTN = "_&ghi"; // Click btn ghi

        public FormKho()
        {
            InitializeComponent();
        }

        private void KhoBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            this.Validate();
            this.bdsKho.EndEdit();
            this.tableAdapterManager.UpdateAll(this.dS);
        }

        private void FormKho_Load(object sender, EventArgs e)
        {
            // Không kiểm tra khóa ngoại
            dS.EnforceConstraints = false;

            this.khoTableAdapter.Connection.ConnectionString = Program.connstr;
            this.khoTableAdapter.Fill(this.dS.Kho);
            this.datHangTableAdapter.Connection.ConnectionString = Program.connstr;
            this.datHangTableAdapter.Fill(this.dS.DatHang);
            this.phieuNhapTableAdapter.Connection.ConnectionString = Program.connstr;
            this.phieuNhapTableAdapter.Fill(this.dS.PhieuNhap);
            this.phieuXuatTableAdapter.Connection.ConnectionString = Program.connstr;
            this.phieuXuatTableAdapter.Fill(this.dS.PhieuXuat);

            if (Program.mGroup == "CONGTY")
            {
                btnThem.Links[0].Visible = btnXoa.Links[0].Visible = btnGhi.Links[0].Visible = btnUndo.Links[0].Visible = false;
            }
            else if (Program.mGroup == "CHINHANH" || Program.mGroup == "USER")
            {
                comboBox_ChiNhanh.Enabled = false;
            }

            maCN = (((DataRowView)bdsKho[0])["MACN"].ToString()); // lúc đúng lúc sai

            this.comboBox_ChiNhanh.DataSource = Program.bds_dspm; // DataSource của comboBox_ChiNhanh tham chiếu đến bindingSource ở LoginForm
            comboBox_ChiNhanh.DisplayMember = "TENCN";
            comboBox_ChiNhanh.ValueMember = "TENSERVER";
            comboBox_ChiNhanh.SelectedIndex = Program.mChinhanh;

            //Mặc định vừa vào groupbox không dx hiện để tránh lỗi sửa các dòng cũ chưa lưu đi qua dòng khác
            btnUndo.Enabled = false;
            //Program.flagCloseFormKho = true; //Khi load bật cho phép có thể đóng form

            history_kho = new Stack<string>();
        }

        private void themFunc()
        {
            position = bdsKho.Position;
            this.bdsKho.AddNew();
            txtMaCN.Text = maCN;
            btnThem.Enabled = btnXoa.Enabled = khoGridControl.Enabled = btnReload.Enabled = false;
            btnUndo.Enabled = gcInfoKho.Enabled = btnGhi.Enabled = true;
        }

        private void BtnThem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            themFunc();
            history_kho.Push(THEM_BTN);
            //Program.flagCloseFormKho = false;    //Bật cờ lên để chặn tắt Form đột ngột khi nhập liệu
        }

        private void BtnXoa_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string maKho = "";
            if (bdsDH.Count > 0)
            {
                MessageBox.Show("Không thể xóa kho vì đã lập đơn đặt hàng", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (bdsPN.Count > 0)
            {
                MessageBox.Show("Không thể xóa kho vì đã lập phiếu nhập", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (bdsPX.Count > 0)
            {
                MessageBox.Show("Không thể xóa kho vì đã lập phiếu xuất", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult dr = MessageBox.Show("Bạn có thực sự muốn xóa kho này không?", "Xác nhận",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (dr == DialogResult.OK)
            {
                try
                {
                    maKho = ((DataRowView)bdsKho[bdsKho.Position])["MAKHO"].ToString();
                    bdsKho.RemoveCurrent();
                    btnUndo.Enabled = true;
                    Program.formMain.timer1.Enabled = true;
                    this.khoTableAdapter.Update(this.dS.Kho);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xảy ra trong quá trình xóa. Vui lòng thử lại!\n" + ex.Message, "Thông báo lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.khoTableAdapter.Fill(this.dS.Kho);
                    bdsKho.Position = bdsKho.Find("MAKHO", maKho);
                    return;
                }
            }

            if (bdsKho.Count == 0) btnXoa.Enabled = false;
        }


        // ------ UNDO ------
        private void unClickThem()
        {
            btnThem.Enabled = btnXoa.Enabled = khoGridControl.Enabled = btnReload.Enabled = true;
            gcInfoKho.Enabled = btnGhi.Enabled = false;
            this.bdsKho.CancelEdit();
            bdsKho.Position = position;
        }

        private void unClickGhi(int index)
        {
            string maKho_backup = ((DataRowView)bdsKho[index])[0].ToString().Trim();
            DialogResult dr = MessageBox.Show("Phiếu '" + maKho_backup + "' đã được ghi vào database.\nBạn có chắc muốn Undo không??", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                //int deletedPosition = current_bds.Find(type, maPhieu);

                string tenKho_backup = ((DataRowView)bdsKho[index])[1].ToString().Trim();
                string diaChi_backup = ((DataRowView)bdsKho[index])[2].ToString().Trim();
                bdsKho.RemoveAt(index);
                themFunc();
                this.khoTableAdapter.Update(this.dS.Kho);
                txtMaKho.Text = maKho_backup;
                txtTenKho.Text = tenKho_backup;
                txtDiaChi.Text = maKho_backup;

                return;
            }

            history_kho.Push(GHI_BTN + "#%" + maKho_backup);
        }

        public int split_index_ghi(string GHIBTN)
        {
            char[] separators = new char[] { '#', '%' };
            string[] temp = GHIBTN.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            string maPhieu = temp[1];
            int indexDataRowUpdated = bdsKho.Find("MAKHO", maPhieu);

            return indexDataRowUpdated;
        }

        // TODO: vị trí, non-update
        private void BtnUndo_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            /*btnThem.Enabled = btnXoa.Enabled = khoGridControl.Enabled = btnReload.Enabled = true;
            btnUndo.Enabled = btnGhi.Enabled = false;
            //Program.flagCloseFormKho = true; //Undo lại thì cho phép thoát mà ko kiểm tra dữ liệu
            bdsKho.CancelEdit();
            bdsKho.Position = position;*/
            String undoHistory = "";
            undoHistory = history_kho.Pop();
            if (history_kho.Count == 0) btnUndo.Enabled = false;

            if (undoHistory.Equals(""))
            {
                btnUndo.Enabled = false;
                return;
            }

            if (undoHistory.Equals(THEM_BTN))
            {
                unClickThem();
                return;
            }

            if (undoHistory.Contains("_&ghi"))
            {
                int index = split_index_ghi(undoHistory);
                unClickGhi(index);
                return;
            }
        }

        private void BtnReload_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.khoTableAdapter.Fill(this.dS.Kho);
        }

        private void BtnThoat_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.Close();
        }

        private void BtnGhi_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Check ràng buộc text box
            if (!Validate(txtMaKho, "Mã kho không được để trống!")) return;
            if (!Validate(txtTenKho, "Tên kho không được để trống!")) return;
            if (!Validate(txtDiaChi, "Địa chỉ không được để trống!")) return;
            if (txtMaKho.Text.Trim().Length > 4)
            {
                MessageBox.Show("Mã kho không được quá 4 kí tự!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (txtMaKho.Text.Contains(" "))
            {
                MessageBox.Show("Mã kho không được chứa khoảng trắng!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // == Query tìm MAKHO ==
            String query_MAKHO = "DECLARE @return_value int " +
                                "EXEC @return_value = [dbo].[SP_CHECKID] " +
                                "@p1, @p2 " +
                                "SELECT 'Return Value' = @return_value";
            SqlCommand sqlCommand = new SqlCommand(query_MAKHO, Program.conn);
            sqlCommand.Parameters.AddWithValue("@p1", txtMaKho.Text);
            sqlCommand.Parameters.AddWithValue("@p2", "MAKHO");
            SqlDataReader dataReader = null;

            try
            {
                dataReader = sqlCommand.ExecuteReader();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Thực thi database thất bại!\n" + ex.Message, "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Đọc và lấy result
            dataReader.Read();
            int result_value_MAKHO = int.Parse(dataReader.GetValue(0).ToString());
            dataReader.Close();

            // == Query tìm TENKHO ==
            String query_TENKHO = "DECLARE @return_value int " +
                                   "EXEC @return_value = [dbo].[SP_CHECKID] " +
                                   "@p1, @p2 " +
                                   "SELECT 'Return Value' = @return_value";
            sqlCommand = new SqlCommand(query_TENKHO, Program.conn);
            sqlCommand.Parameters.AddWithValue("@p1", txtTenKho.Text);
            sqlCommand.Parameters.AddWithValue("@p2", "TENKHO");

            try
            {
                dataReader = sqlCommand.ExecuteReader();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Thực thi database thất bại!\n" + ex.Message, "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Đọc và lấy result
            dataReader.Read();
            int result_value_TENKHO = int.Parse(dataReader.GetValue(0).ToString());
            dataReader.Close();

            // Check ràng buộc MAKHO, TENKHO
            int indexMaKHO = bdsKho.Find("MAKHO", txtMaKho.Text);
            int indexTENKHO = bdsKho.Find("TENKHO", txtTenKho.Text);
            int indexCurrent = bdsKho.Position;
            if (result_value_MAKHO == 1 && (indexMaKHO != indexCurrent))
            {
                MessageBox.Show("Mã kho đã tồn tại ở chi chánh hiện tại!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (result_value_TENKHO == 1 && (indexTENKHO != indexCurrent))
            {
                MessageBox.Show("Tên kho đã tồn tại ở chi nhánh hiện tại!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (result_value_MAKHO == 2)
            {
                MessageBox.Show("Mã kho đã tồn tại ở chi nhánh khác!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (result_value_TENKHO == 2)
            {
                MessageBox.Show("Tên kho đã tồn tại ở chi nhánh khác!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                DialogResult dr = MessageBox.Show("Bạn có chắc muốn ghi dữ liệu vào Database?", "Thông báo",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.OK)
                {
                    try
                    {
                        //Program.flagCloseFormKho = true; //Bật cờ cho phép tắt Form NV
                        btnThem.Enabled = btnXoa.Enabled = khoGridControl.Enabled = gcInfoKho.Enabled = true;
                        btnReload.Enabled = btnGhi.Enabled = true;
                        btnUndo.Enabled = true;
                        this.bdsKho.EndEdit();
                        Program.formMain.timer1.Enabled = true;
                        this.khoTableAdapter.Update(this.dS.Kho);
                        history_kho.Push(GHI_BTN + "#%" + txtMaKho.Text);
                        bdsKho.Position = position;
                    }
                    catch (Exception ex)
                    {
                        // Khi Update database lỗi thì xóa record vừa thêm trong bds
                        bdsKho.RemoveCurrent();
                        MessageBox.Show("Thất bại. Vui lòng kiểm tra lại!\n" + ex.Message, "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ComboBox_ChiNhanh_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Trường hợp chưa kịp chọn CN, thuộc tính index ở combobox sẽ thay đổi
            // "System.Data.DataRowView" sẽ xuất hiện và tất nhiên hệ thống sẽ không thể
            // nhận diện được tên server "System.Data.DataRowView".
            if (comboBox_ChiNhanh.SelectedValue.ToString() == "System.Data.DataRowView") return;

            // Lấy tên server
            Program.serverName = comboBox_ChiNhanh.SelectedValue.ToString();

            // Nếu tên server khác với tên server ngoài đăng nhập, thì ta phải sử dụng HTKN
            if (comboBox_ChiNhanh.SelectedIndex != Program.mChinhanh)
            {
                Program.mlogin = Program.remoteLogin;
                Program.password = Program.remotePassword;
            }
            else
            {
                Program.mlogin = Program.mloginDN;
                Program.password = Program.passwordDN;
            }

            if (Program.KetNoi() == 0)
                MessageBox.Show("Lỗi kết nối về chi nhánh mới", "", MessageBoxButtons.OK);
            else
            {
                this.khoTableAdapter.Connection.ConnectionString = Program.connstr;
                this.khoTableAdapter.Fill(this.dS.Kho);
                maCN = ((DataRowView)bdsKho[0])["MACN"].ToString();
            }
        }

        private bool Validate(TextBox tb, string str)
        {
            if (tb.Text.Trim().Equals(""))
            {
                MessageBox.Show(str, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus();
                return false;
            }
            return true;
        }
    }
}

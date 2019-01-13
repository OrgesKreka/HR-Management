using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HRApplication
{
    public partial class UserShiftFForm : Form
    {
        /// <summary>
        /// Flag per te kuptuar nese perdoresi ka dale nga forma me butonin logout
        /// apo ka mbyllur programin me butonin X
        /// nese del me logout e con te forma paraardhese e logimit
        /// </summary>
        public bool UserLogedOut { get; private set; } = false;

        private TurnStatus _statusOfShift;

        public UserShiftFForm()
        {
            InitializeComponent();
            this.Text = $"{Helper.LogedUser.UserName} - Shift";
            var image = Image.FromStream(new System.IO.MemoryStream(Helper.LogedUser.ProfileImage)); //ByteToImage(Helper.LogedUser.ProfileImage);
            profilePictureBox.Image = Helper.ResizeImage(image, profilePictureBox.Size);
            nameLabel.Text = Helper.LogedUser.UserName;

            dateLabel.Text = DateTime.UtcNow.Date.ToString("dd MM yyyy ");
            timeLabel.Text = DateTime.UtcNow.ToString("hh:mm:ss");

            if (Helper.LogedUser.TurnStatus == TurnStatus.Open)
            {
                shiftButton.Text = "End Shift";
                _statusOfShift = TurnStatus.Open; 
             }
            else
            {
               shiftButton.Text= "Start Shift";
                _statusOfShift = TurnStatus.Closed;
            }

            Fill5RecentPunches();
        }


        /// <summary>
        ///  Merr 5 turnet e fundit dhe i shfaq
        /// </summary>
        private void Fill5RecentPunches()
        {
            using (var connection = new SQLiteConnection(Helper.ConnectionString))
            {
                connection.Open();

                var sqlcommand = new SQLiteCommand(@"select * from users u
                                                    inner join sessions s
                                                    on u.id = s.userid
                                                    inner join startingtimetable t1
                                                    on s.id = t1.sessionid
                                                    inner join endingtimetable t2 
                                                    on t2.sessionid = s.id
                                                    limit 5", connection);

                var reader = sqlcommand.ExecuteReader();

                int i = 0;
                while(reader.Read())
                {
                    // Kolona e 1 ne tabele
                    var date = DateTime.ParseExact( reader["starttime"].ToString(), "dd MM yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString("MMMM dd");
                    var paneli1 = tableLayoutPanel2.Controls.Find($"panelt{i}1", true)[0];
                    paneli1.Controls.Add( new Label { Text = date});

                    var paneli2 = (Panel)tableLayoutPanel2.Controls.Find($"panelt{i}2",true)[0];
                    paneli2.Controls.Add(new Label { Text =Helper.LogedUser.UserName});

                    var starttime = DateTime.ParseExact(reader["starttime"].ToString(), "dd MM yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString("hh:mm tt");
                    var paneli3 = (Panel)tableLayoutPanel2.Controls.Find($"panelt{i}3", true)[0];
                    paneli3.Controls.Add(new Label { Text = starttime});

                    var endtime = DateTime.ParseExact(reader["endingtime"].ToString(), "dd MM yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString("hh:mm tt");
                    var paneli4 = (Panel)tableLayoutPanel2.Controls.Find($"panelt{i}4", true)[0];
                    paneli4.Controls.Add(new Label { Text = endtime});
                    i++;

                }
            }
        }

        private void logoutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UserLogedOut = true;
            this.Close();
        }

        private void shiftButton_Click(object sender, EventArgs e)
        {
            if(_statusOfShift == TurnStatus.Closed)
            {
                shiftButton.Text = "Close Shift";
                StartShift();
                _statusOfShift = TurnStatus.Open;
            }
            else
            {
                shiftButton.Text = "Open Shift";
                EndShift();
                _statusOfShift = TurnStatus.Closed;
            }

            shiftButton.Refresh();
        }

        private void StartShift()
        {

            // Fillon sesionin e ri
            using (var connection = new SQLiteConnection(Helper.ConnectionString))
            {
                connection.Open();

                var sqlInsert = new SQLiteCommand(@"INSERT INTO sessions( userid, sessioncode) 
                                                VALUES( @userid, @sessioncode );
                                                select last_insert_rowid()", connection);

                sqlInsert.Parameters.AddWithValue("@sessioncode", Helper.LogedUser.SessionCode+1);
                sqlInsert.Parameters.AddWithValue("@userid", Helper.LogedUser.UserId);

                var result = sqlInsert.ExecuteScalar();
                Helper.LogedUser.SessionId = Convert.ToInt32(result);
            }



            // pastaj fillon turnin
            using (var connection = new SQLiteConnection(Helper.ConnectionString))
            {
                connection.Open();

                var sqlInsert = new SQLiteCommand( @"INSERT INTO startingtimetable( sessionId, userId, startTime ) 
                                                VALUES( @sessionId, @userId, @startTime )", connection);

                sqlInsert.Parameters.AddWithValue(  "@sessionId", Helper.LogedUser.SessionId);
                sqlInsert.Parameters.AddWithValue( "@userId", Helper.LogedUser.UserId );
                sqlInsert.Parameters.AddWithValue( "@startTime", DateTime.Now.ToString("dd MM yyyy hh:mm:ss") );

               var result = sqlInsert.ExecuteNonQuery();
            }
        }

        private void EndShift()
        {
            using (var connection = new SQLiteConnection(Helper.ConnectionString))
            {
                connection.Open();

                var sqlInsert = new SQLiteCommand(@"INSERT INTO endingtimetable( sessionId, userId, endingtime ) 
                                                VALUES( @sessionId, @userId, @endingTime )", connection);

                sqlInsert.Parameters.AddWithValue("@sessionId", Helper.LogedUser.SessionId);
                sqlInsert.Parameters.AddWithValue("@userId", Helper.LogedUser.UserId);
                sqlInsert.Parameters.AddWithValue("@endingTime", DateTime.Now.ToString("dd MM yyyy hh:mm:ss"));

                var result = sqlInsert.ExecuteNonQuery();
            }
        }
    }
}

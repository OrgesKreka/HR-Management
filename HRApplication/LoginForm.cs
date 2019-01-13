using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HRApplication
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();

            if (Helper.IsDebuggerAttached)
            {
                usernameTextField.Text = "user1";
                passwordTextField.Text = "user1";

            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //Shtese jofunksionale, supozohet te te coje tek faqja web per te bere reset fjalekalimin
            // ose ne varesi te politikes se kompanise, te beje kerkese tek supervizori per ta ndryshuar.

            if( string.IsNullOrEmpty( usernameTextField.Text))
            {
                MessageBox.Show("Emri i perdoruesit eshte i detyruar!");
                return;
            }

            System.Diagnostics.Process.Start("https://www.website.com/resetpassword");
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            // Kontroll per vlerat bosh.
            var userName = usernameTextField.Text;
            var password = passwordTextField.Text;

            if (string.IsNullOrEmpty(userName))
            {
                MessageBox.Show("Emri eshte fushe e detyruar!", "Input i pavlefshem",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if(string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Fjalekalimi eshte fushe e detyruar!", "Input i pavlefshem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kontollon nese perdoruesi ndodhet ne database
            using (var connection = new SQLiteConnection(Helper.ConnectionString) )
            {
                connection.Open();

                // Merr te dhenat e userit dhe sessionit te fundit qe ka krijuar ky useri
                var checkUserQuery = @"SELECT * FROM users 
                    left join sessions 
                    on users.id = sessions.userid
                    WHERE username = @userName AND password = @password";

                var command = new SQLiteCommand(checkUserQuery, connection);
                command.Parameters.AddWithValue("@userName", userName);
                command.Parameters.AddWithValue("@password", password);

                var reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    Helper.LogedUser = new Models.User();
                }
                else
                {
                    // Mesazhi qe perdoruesi nuk ekziston
                    MessageBox.Show($"{userName} nuk ekziston!", "Perdoruesi nuk ekziston", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Map rezultatin me modelin
                while(reader.Read())
                {
                    Helper.LogedUser.UserId =  Convert.ToInt32( reader[0] );
                    Helper.LogedUser.UserName = reader[1].ToString();
                    Helper.LogedUser.Password = reader[2].ToString();
                    Helper.LogedUser.ProfileImage =  reader[3].ToString().Equals(DBNull.Value.ToString()) ? File.ReadAllBytes( Path.Combine(Environment.CurrentDirectory, "Resources", "empty-user.ico" )) : reader[3].ToString().Select(s => Byte.Parse(s.ToString(),
                                           NumberStyles.HexNumber,
                                           CultureInfo.InvariantCulture)
                          ).ToArray(); ;

                    Helper.LogedUser.SessionCode = reader[6].ToString().Equals(DBNull.Value.ToString()) ? 1 : Convert.ToInt32(reader[6].ToString());
                    Helper.LogedUser.SessionId = reader[4].ToString().Equals(DBNull.Value.ToString()) ? 1 : Convert.ToInt32(reader[4].ToString());

                    // per te percaktuar statusin e turnit, ben query tek tabela startingtime, per te pare nese ka sesion te hapur perdoruesi apo jo
                    command = new SQLiteCommand("SELECT * FROM startingtimetable WHERE sessionid = @sessionId AND userid = @userid", connection);
                    command.Parameters.AddWithValue("@sessionId", Helper.LogedUser.SessionId);
                    command.Parameters.AddWithValue("@userId", Helper.LogedUser.UserId);
                    using (var tmp = command.ExecuteReader())
                    {
                        Helper.LogedUser.TurnStatus = tmp.HasRows ? TurnStatus.Open : TurnStatus.Closed;
                    }

                    // Kontrollon edhe tek endingtimetale
                    command = new SQLiteCommand("SELECT * FROM endingtimetable WHERE sessionid = @sessionId AND userid = @userid", connection);
                    command.Parameters.AddWithValue("@sessionId", Helper.LogedUser.SessionId);
                    command.Parameters.AddWithValue( "@userId", Helper.LogedUser.UserId );
                    using (var tmp = command.ExecuteReader())
                    {
                        if (tmp.HasRows)
                            Helper.LogedUser.TurnStatus = TurnStatus.Closed;
                    }
                }

                if(Helper.IsDebuggerAttached)
                    Debug.WriteLine( Helper.LogedUser.ToString);

                var userLogedFromShiftForm = true;

                this.Hide();
                using (var userForm = new UserShiftFForm())
                {
                    userForm.ShowDialog();
                    userLogedFromShiftForm = userForm.UserLogedOut;
                }

                if (userLogedFromShiftForm)
                    this.Show();
                else
                    this.Close();
            }
        }
    }
}

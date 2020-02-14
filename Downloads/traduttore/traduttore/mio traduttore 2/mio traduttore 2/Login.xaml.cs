using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace mio_traduttore_2
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private string pswd, usr;

        private string constr;

        // use to perform read and write operations in the database 
        private SqlCommand cmd;

        //use to read a row in table one by one
        private SqlDataReader dreader;

        // for the connection to 
        // sql server database 
        private SqlConnection conn;

        // to sore SQL command and the output of SQL command 
        private string sql;

        public Window1()
        {
            // Data Source is the name of the 
            // server on which the database is stored. 
            // The Initial Catalog is used to specify 
            // the name of the database 
            // The UserID and Password are the credentials 
            // required to connect to the database. 
            constr = @"Server=tcp:traduttoredb.database.windows.net,1433;Initial Catalog=TraduttoreDB;Persist Security Info=False;
                User ID=admintraduttore;Password=Settimana1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            conn = new SqlConnection(constr);

            InitializeComponent();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectUser())
            {
                error.Content = "";
                while (dreader.Read())  //controlla nel DB che l'utente non sia già registrato controllando la sua mail
                {
                    if (dreader.GetValue(0).Equals(usr))
                    {
                        error.Content = "UTENTE GIA' REGISTRATO";
                        return;

                    }
                }
                //superati i controlli nel DB, inserisci il nuovo utente
                dreader.Close();
                sql = "INSERT INTO Utente (Email, Password) VALUES('" + usr + "', '" + pswd + "');";

                // to execute the sql statement 
                cmd = new SqlCommand(sql, conn);

                cmd.ExecuteNonQuery();

                error.Content = "REGISTRATO, ACCEDI AI SERVIZI";

                conn.Close();
            }

            error.Content = "USERNAME O PASSWORD ERRATI";
            Console.WriteLine("USERNAME O PASSWORD ERRATI!");
        }

        private void LogIn(object sender, RoutedEventArgs e)
        {
            if (SelectUser())
            {
                error.Content = "";
                // for one by one reading row 
                while (dreader.Read())
                {
                    if (dreader.GetValue(0).Equals(usr) && dreader.GetValue(1).Equals(pswd))
                    {
                        Console.WriteLine("USERNAME E PASSWORD CORRETTI... REDIRECT");
                        conn.Close();
                        dreader.Close();
                        cmd.Dispose();
                        Switch();
                        return;

                    }

                }
            }

            error.Content = "USERNAME O PASSWORD ERRATI";
            Console.WriteLine("USERNAME O PASSWORD ERRATI!");
        }

        private void Switch()
        {       
           var a=new MainWindow(emailText.Text, passwordBox.Password);
            a.Show();
            this.Close();
        }

        private bool SelectUser()
        {
            conn.Close();
            usr = emailText.Text;
            pswd = passwordBox.Password;

            Regex regex = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*"
            + "@"
            + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
            Match match = regex.Match(usr);
            if (!match.Success)
            {
                Console.WriteLine("Email non valida");
                conn.Close();
                return false;
            }

            conn.Open();

            // use to fetch rwos from demo table 
            sql = "Select Email, Password from Utente";

            // to execute the sql statement 
            cmd = new SqlCommand(sql, conn);

            // fetch all the rows from the demo table 
            dreader = cmd.ExecuteReader();

            return true;
        }
    }
}

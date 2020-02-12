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

namespace mio_traduttore_2
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        string pswd, usr;
        int i, j;

        public Window1()
        {
            InitializeComponent();
        }

        //salva l'email inserita nella form in una variabile
        private void EmailText_TextChanged(object sender, TextChangedEventArgs e)
        {
            usr = sender.ToString();
            j = usr.IndexOf(' ');  //trova l'occorrenza dello spazio nella stringa
            j++;
            usr = usr.Substring(j);   //usr ora contiene solo l'email dello user

        }

        //salva la pass inserita nella form in una variabile
        private void PasswordText_TextChanged(object sender, TextChangedEventArgs e)
        {
            pswd = sender.ToString();
            i = pswd.IndexOf(' ');  //trova l'occorrenza dello spazio nella stringa
            i++;                       //incremento i pxer portarlo al valore che ci serve
            pswd = pswd.Substring(i);   //pswd ora contiene solo la password
        }

        private void LogIn(object sender, RoutedEventArgs e)
        {
            string constr;

            // for the connection to 
            // sql server database 
            SqlConnection conn;

            // Data Source is the name of the 
            // server on which the database is stored. 
            // The Initial Catalog is used to specify 
            // the name of the database 
            // The UserID and Password are the credentials 
            // required to connect to the database. 
            constr = @"Server=tcp:traduttoredb.database.windows.net,1433;Initial Catalog=TraduttoreDB;Persist Security Info=False;
                User ID=admintraduttore;Password=Settimana1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            conn = new SqlConnection(constr);

            conn.Open();

            // use to perform read and write operations in the database 
            SqlCommand cmd;

            //use to read a row in table one by one
            SqlDataReader dreader;

            // to sore SQL command and the output of SQL command 
            string sql, output = "";

            // use to fetch rwos from demo table 
            sql = "Select Email, Password from Utente";

            // to execute the sql statement 
            cmd = new SqlCommand(sql, conn);

            // fetch all the rows from the demo table 
            dreader = cmd.ExecuteReader();

            // for one by one reading row 
            while (dreader.Read())
            {

                if (dreader.GetValue(0).Equals(usr) && dreader.GetValue(1).Equals(pswd))
                {
                    Console.WriteLine("USERNAME E PASSWORD CORRETTI, FAI IL REDIRECT");
                    Switch();
                    dreader.Close();
                    cmd.Dispose();
                    conn.Close();
                    return;
                }
            }

            // to close all the objects 
            dreader.Close();
            cmd.Dispose();
            conn.Close();
            error.Content = "USERNAME O PASSWORD ERRATI!";
            Console.WriteLine("USERNAME O PASSWORD ERRATI!");
        }

        private void Switch()
        {       //PASSARE LE VARIABILI USR E PSWD AL MAIN WINDOW
           new MainWindow(EmailText.Text,PasswordText.Text);
            
            this.Close();
        }
    }
}

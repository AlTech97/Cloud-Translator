using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace mio_traduttore_2
{
    class Cronologia{
        private String email;
        // for the connection to sql server database 
        private SqlConnection conn;
        private String constr;

        //use to read a row in table one by one
        private SqlDataReader dreader;

        // use to perform read and write operations in the database 
        private SqlCommand cmd;

        // to sore SQL command and the output of SQL command 
        private string sql;


        public Cronologia(String e)
        {
            email = e;

            // Data Source is the name of the 
            // server on which the database is stored. 
            // The Initial Catalog is used to specify 
            // the name of the database 
            // The UserID and Password are the credentials 
            // required to connect to the database. 
            constr = @"Server=tcp:traduttoredb.database.windows.net,1433;Initial Catalog=TraduttoreDB;Persist Security Info=False;
                User ID=admintraduttore;Password=Settimana1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            conn = new SqlConnection(constr);
            
        }

        public ArrayList getCronologia()
        {
            String[] riga = new String[2];          //per conservare una riga di 2 elementi da aggiungere nell'arraylist di righe
            ArrayList risultato= new ArrayList();
          
            // Seleziona il testo da tradurre e il testo tradotto dall'utente (identificato con email)
            //e ordina i risultati dal più recente al meno recente
            sql = "SELECT DaTradurre, Tradotto FROM Cronologia WHERE Email = '"+ email +"' ORDER BY timestmp DESC";

            conn.Open();

            // to execute the sql statement 
            cmd = new SqlCommand(sql, conn);

            // fetch all the rows from the demo table 
            dreader = cmd.ExecuteReader();
            
            //salva i risultati della query in una matrice e restituiscila
            while(dreader.Read())
            {
                riga = new String[2];
                riga[0] = (String) dreader.GetValue(0); //da tradurre
                riga[1] = (String)dreader.GetValue(1);    //tradotto
               
                risultato.Add(riga);

            }
            conn.Close();
            return risultato;
        }

        public bool setCronologia(String email, String daTradurre, String traddotto)
        {
            
            conn.Open();
            sql = "INSERT INTO Cronologia (Email, DaTradurre, Tradotto) VALUES('"+ email +"', "+ "'"+ daTradurre + "', "+ "'"+ traddotto +"');" ;
            cmd = new SqlCommand(sql, conn);

            if (cmd.ExecuteNonQuery() > 0)
            {
                conn.Close();
                return true;
            }
            else
            {
                conn.Close();
                return false;
            }



            
        }
       
    }
}

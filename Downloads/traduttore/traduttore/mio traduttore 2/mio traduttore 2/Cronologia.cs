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

        public Cronologia(String e)
        {
            email = e;
        }

        public ArrayList getCronologia()
        {
            String[] riga = new String[2];
            ArrayList risultato= new ArrayList();
            String constr;

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
            string sql;

            // Seleziona il testo da tradurre e il testo tradotto dall'utente (identificato con email)
            //e ordina i risultati dal più recente al meno recente
            sql = "SELECT DaTradurre, Tradotto FROM Cronologia WHERE Email = "+ email +" ORDER BY timestmp DESC";

            // to execute the sql statement 
            cmd = new SqlCommand(sql, conn);

            // fetch all the rows from the demo table 
            dreader = cmd.ExecuteReader();
            
            //salva i risultati della query in una matrice e restituiscila
            while(dreader.Read())
            {
                riga[0] = (String) dreader.GetValue(0); //da tradurre
                riga[1] = (String)dreader.GetValue(1);    //tradotto
                risultato.Add(riga);

            }
            return risultato;
        }
       
    }
}

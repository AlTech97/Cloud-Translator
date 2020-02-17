using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Net;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Web;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Microsoft.Win32;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.CognitiveServices.Speech;

using NAudio.Wave;

using LitJson;

using System.Diagnostics;
using System;
//using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using Microsoft.VisualBasic;
using System.Data;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.Windows.Controls;

namespace mio_traduttore_2
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int record(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        // This sample uses the Cognitive Services subscription key for all services. To learn more about
        // authentication options, see:
        const string COGNITIVE_SERVICES_KEY = "77ba3cf1243641b9a26210bbfcf65e64";
        // Endpoints for Translator Text and Bing Spell Check
        public static readonly string TEXT_TRANSLATION_API_ENDPOINT = "https://api.cognitive.microsofttranslator.com/{0}?api-version=3.0";
        const string BING_SPELL_CHECK_API_ENDPOINT = "https://westeurope.api.cognitive.microsoft.com/bing/v7.0/spellcheck/";
        // An array of language codes
        private string[] languageCodes;
        public string Testo,email,psw;
        public static bool alreadyRecording = false, stopRecording = false;
        public static string textDetected, detectedLanguage, fromLanguage,toLanguage;
        private Cronologia cache;
        ArrayList appoggio;
        // Dictionary to map language codes from friendly name (sorted case-insensitively on language name)antonio@libero.it antopassword
        private SortedDictionary<string, string> languageCodesAndTitles =
            new SortedDictionary<string, string>(Comparer<string>.Create((a, b) => string.Compare(a, b, true)));
        public static string cammino = @"C:\Users\"+Environment.UserName+"\\Desktop\\";
            //System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("\\mio traduttore 2\\bin\\x64\\Debug\\mio traduttore 2.exe", "");

        /*public MainWindow(String mail,String password) {
            email = mail;
            psw = password;
            
            var win = new MainWindow();
            win.Show();
        }*/
        public MainWindow() {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExceptions);
            var log = new Window1();
            log.Show();
            this.Close();
        }
        public MainWindow(String mail, String password)
        {
            // at least show an error dialog if there's an unexpected error
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExceptions);
            email = mail;
            psw = password;
            
            if (email != null)
            {
                cache = new Cronologia(email);
                Console.WriteLine(email);
            }
            if (COGNITIVE_SERVICES_KEY.Length != 32)
            {
                MessageBox.Show("One or more invalid API subscription keys.\n\n" +
                    "Put your keys in the *_API_SUBSCRIPTION_KEY variables in MainWindow.xaml.cs.",
                    "Invalid Subscription Key(s)", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                // Start GUI
                InitializeComponent();
                // Get languages for drop-downs
                GetLanguagesForTranslate();
                // Populate drop-downs with values from GetLanguagesForTranslate
                PopulateLanguageMenus();
                // Set user
                setUser();

                Button_stopRecord.IsEnabled = false;
                Button_stopRecord.Visibility = Visibility.Collapsed;
            }
        }

        // Global exception handler to display error message and exit
        private static void HandleExceptions(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show("Caught " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown();
        }

        private void setUser()
        {
            currentUser.Content = "Current user: " + email;
        }

        // ***** POPULATE LANGUAGE MENUS
        private void PopulateLanguageMenus()
        {
            // Add option to automatically detect the source language
            FromLanguageComboBox.Items.Add("Detect");

            int count = languageCodesAndTitles.Count;
            foreach (string menuItem in languageCodesAndTitles.Keys)
            {
                FromLanguageComboBox.Items.Add(menuItem);
                ToLanguageComboBox.Items.Add(menuItem);
            }

            // Set default languages
            FromLanguageComboBox.SelectedItem = "Detect";
            ToLanguageComboBox.SelectedItem = "Italian";
            appoggio = cache.getCronologia();
        }

        // ***** DETECT LANGUAGE OF TEXT TO BE TRANSLATED
        private string DetectLanguage(string text)
        {
            string detectUri = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "detect");

            // Create request to Detect languages with Translator Text
            HttpWebRequest detectLanguageWebRequest = (HttpWebRequest)WebRequest.Create(detectUri);
            detectLanguageWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
            detectLanguageWebRequest.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");
            detectLanguageWebRequest.ContentType = "application/json; charset=utf-8";
            detectLanguageWebRequest.Method = "POST";

            // Send request
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonText = serializer.Serialize(text);

            string body = "[{ \"Text\": " + jsonText + " }]";
            byte[] data = Encoding.UTF8.GetBytes(body);

            detectLanguageWebRequest.ContentLength = data.Length;

            using (var requestStream = detectLanguageWebRequest.GetRequestStream())
                requestStream.Write(data, 0, data.Length);

            HttpWebResponse response = (HttpWebResponse)detectLanguageWebRequest.GetResponse();
            
            // Read and parse JSON response
            var responseStream = response.GetResponseStream();
            var jsonString = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
            dynamic jsonResponse = serializer.DeserializeObject(jsonString);

            // Fish out the detected language code
            var languageInfo = jsonResponse[0];
            if (languageInfo["score"] > (decimal)0.5)
            {
                DetectedLanguageLabel.Content = "Detected language: "+languageInfo["language"];
                return languageInfo["language"];
            }
            else
                return "Unable to confidently detect input language.";
        }

        // ***** CORRECT SPELLING OF TEXT TO BE TRANSLATED
        private string CorrectSpelling(string text)
        {
            string uri = BING_SPELL_CHECK_API_ENDPOINT + "?mode=spell&mkt=en-US";

            // Create a request to Bing Spell Check API
            HttpWebRequest spellCheckWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            spellCheckWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
            spellCheckWebRequest.Method = "POST";
            spellCheckWebRequest.ContentType = "application/x-www-form-urlencoded"; // doesn't work without this
            
            // Create and send the request
            string body = "text=" + System.Web.HttpUtility.UrlEncode(text);
            byte[] data = Encoding.UTF8.GetBytes(body);
            spellCheckWebRequest.ContentLength = data.Length;
            using (var requestStream = spellCheckWebRequest.GetRequestStream())
                requestStream.Write(data, 0, data.Length);
            HttpWebResponse response = (HttpWebResponse)spellCheckWebRequest.GetResponse();

            // Read and parse the JSON response; get spelling corrections
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var responseStream = response.GetResponseStream();
            var jsonString = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
            dynamic jsonResponse = serializer.DeserializeObject(jsonString);
            var flaggedTokens = jsonResponse["flaggedTokens"];

            // Construct sorted dictionary of corrections in reverse order (right to left)
            // This ensures that changes don't impact later indexes
            var corrections = new SortedDictionary<int, string[]>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            for (int i = 0; i < flaggedTokens.Length; i++)
            {
                var correction = flaggedTokens[i];
                var suggestion = correction["suggestions"][0];  // consider only first suggestion
                if (suggestion["score"] > (decimal)0.7)         // take it only if highly confident
                    corrections[(int)correction["offset"]] = new string[]   // dict key   = offset
                        { correction["token"], suggestion["suggestion"] };  // dict value = {error, correction}
            }

            // Apply spelling corrections, in order, from right to left
            foreach (int i in corrections.Keys)
            {
                var oldtext = corrections[i][0];
                var newtext = corrections[i][1];

                // Apply capitalization from original text to correction - all caps or initial caps
                if (text.Substring(i, oldtext.Length).All(char.IsUpper)) newtext = newtext.ToUpper();
                else if (char.IsUpper(text[i])) newtext = newtext[0].ToString().ToUpper() + newtext.Substring(1);

                text = text.Substring(0, i) + newtext + text.Substring(i + oldtext.Length);
            }

            return text;
        }

        // ***** GET TRANSLATABLE LANGUAGE CODES
        private void GetLanguagesForTranslate()
        {
            // Send a request to get supported language codes
            string uri = String.Format(TEXT_TRANSLATION_API_ENDPOINT, "languages") + "&scope=translation";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
            WebRequest.Headers.Add("Accept-Language", "en");
            WebResponse response = null;
            // Read and parse the JSON response
            response = WebRequest.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream(), UnicodeEncoding.UTF8))
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(reader.ReadToEnd());
                var languages = result["translation"];

                languageCodes = languages.Keys.ToArray();
                foreach (var kv in languages)
                {
                    languageCodesAndTitles.Add(kv.Value["name"], kv.Key);
                }
            }
        }

        // ***** PERFORM TRANSLATION ON BUTTON CLICK
        private async void TranslateButton_Click(object sender, EventArgs e)
        {
            string textToTranslate = TextToTranslate.Text.Trim();

            string fromLanguage = FromLanguageComboBox.SelectedValue.ToString();
            string fromLanguageCode;

            // Auto-detect source language if requested
            if (fromLanguage == "Detect")
            {
                fromLanguageCode = DetectLanguage(textToTranslate);
                if (!languageCodes.Contains(fromLanguageCode))
                {
                    MessageBox.Show("The source language could not be detected automatically " +
                        "or is not supported for translation.", "Language detection failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
                fromLanguageCode = languageCodesAndTitles[fromLanguage];

            string toLanguageCode = languageCodesAndTitles[ToLanguageComboBox.SelectedValue.ToString()];

            // Spell-check the source text if the source language is English
            if (fromLanguageCode == "en")
            {
                if (textToTranslate.StartsWith("-"))    // don't spell check in this case
                    textToTranslate = textToTranslate.Substring(1);
                else
                {
                    textToTranslate = CorrectSpelling(textToTranslate);
                    TextToTranslate.Text = textToTranslate;     // put corrected text into input field
                }
            }

            // Handle null operations: no text or same source/target languages
            if (textToTranslate == "" || fromLanguageCode == toLanguageCode)
            {
                TranslatedTextLabel.Content = textToTranslate;
                return;
            }

            // Send translation request
            string endpoint = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "translate");
            string uri = string.Format(endpoint + "&from={0}&to={1}", fromLanguageCode, toLanguageCode);

            System.Object[] body = new System.Object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");
                request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody);
                var translation = result[0]["translations"][0]["text"];

                // Update the translation field
                TranslatedTextLabel.Content = translation;
            }
            controlCache(TextToTranslate.Text);
            detectedLanguage = ToLanguageComboBox.SelectedItem.ToString();
        }
        public void controlCache(String a) {
            int flag = 0;
            foreach (String[] b in appoggio)
            {

                //Console.WriteLine(a[0] + "" + a[1]);
                
                if (b[0].Equals(a))
                {
                    flag = 1;
                }
                    
            }
            if (flag == 0) {
                string[] s = new string[2];
                s[0] = a;
                s[1]=TranslatedTextLabel.Content.ToString();
                cache.setCronologia(email, s[0], s[1]);
                appoggio.Insert(0, s);
            }
            }
        private async void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog opnfd = new OpenFileDialog();
            opnfd.Filter = "Image Files (*.jpg;*.jpeg;.*.gif;)|*.jpg;*.jpeg;.*.gif";
            
            if (opnfd.ShowDialog() != false)
            {
                Console.Out.WriteLine(opnfd.FileName);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri(opnfd.FileName);
                immagine.Source = bitmapImage;
                
                TextToTranslate.Text= await MakeRequest2(opnfd.FileName);
                TranslateButton_Click(sender, e);
            }


        }
        
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }
       
       
        
        static async Task<String> MakeRequest2(string path)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);

            var uri = "https://westeurope.api.cognitive.microsoft.com/vision/v2.0/read/core/asyncBatchAnalyze?" + queryString;

            HttpResponseMessage response;
            string operationLocation;
            // Request body
            byte[] byteData =GetImageAsByteArray(path);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);
                Console.WriteLine(response.ToString());
            }
            if (response.IsSuccessStatusCode)
            {
                operationLocation =
                    response.Headers.GetValues("Operation-Location").FirstOrDefault();
                return await sendLocationalAsync(operationLocation);
            }
            else
            {
                // Display the JSON error data.
                string errorString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\n\nResponse:\n{0}\n",
                    JToken.Parse(errorString).ToString());
                return "error";
            }

        }

        static async Task<string> sendLocationalAsync(string operationLocation) {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            //operationLocation = operationLocation.Replace();
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",  COGNITIVE_SERVICES_KEY);

            var uri = operationLocation + queryString;

            var response = await client.GetAsync(uri);
            var a = await response.Content.ReadAsStringAsync();
            //  var contents = await response.Content.ReadAsStringAsync();
            while (a.Equals("{\"status\":\"Running\"}"))
            {
                Thread.Sleep(3500);
                response = await client.GetAsync(uri);
                a = await response.Content.ReadAsStringAsync();
                
            }
            if (response.IsSuccessStatusCode)
            {
                a = await response.Content.ReadAsStringAsync();
                Console.WriteLine(a);
                //var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, List<Dictionary<string, List<Dictionary<string, string>>>>>>>>>(a);
                var result = JsonMapper.ToObject(a);
                //var translation = result[0]["recognitionResults"][0]["lines"][0]["text"];
                var tentativo = "";
                //var b=tentativo["Lines"];
                var linee = JsonMapper.ToJson(result["recognitionResults"][0]["lines"]);
                
                for (int i = 0; i < linee.Length; i++)
                {
                    try
                    {
                        
                        tentativo = String.Concat(tentativo, " \n", result["recognitionResults"][0]["lines"][i]["text"]);
                       
                    }
                    catch
                    {
                        break;
                    }
                }

                return tentativo.ToString();


            }
            else
            {
                // Display the JSON error data.
                string errorString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\n\nResponse:\n{0}\n",
                    JToken.Parse(errorString).ToString());
                return "error";
            }
           // Console.WriteLine(contents[0]);
        }

        private async void ascolta(object sender, EventArgs e)
        {
            await SynthesisToSpeakerAsync((string)TranslatedTextLabel.Content);
        }

        private async void textToSpeech(object sender, EventArgs e)
        {
            fromLanguage = FromLanguageComboBox.SelectedItem.ToString();

            if (!alreadyRecording)
            {
                alreadyRecording = true;
                stopRecording = false;
                Button_startRecord.IsEnabled = false;
                Button_startRecord.Visibility = Visibility.Collapsed;
                Button_stopRecord.IsEnabled = true;
                Button_stopRecord.Visibility = Visibility.Visible;
                StartRecordAudio();
            }
            else
            {
                alreadyRecording = false;
                stopRecording = true;
                Button_startRecord.IsEnabled = true;
                Button_startRecord.Visibility = Visibility.Visible;
                Button_stopRecord.IsEnabled = false;
                Button_stopRecord.Visibility = Visibility.Collapsed;
                StopRecordAudio();
                await SynthesisToTextAsync();
                setDetectedText(textDetected);
            }
        }

        public static async Task SynthesisToSpeakerAsync(string testo)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            // The default language is "en-us".
            var config = SpeechConfig.FromSubscription(COGNITIVE_SERVICES_KEY, "westeurope");
            if (detectedLanguage != null)
            {
                if (detectedLanguage.Equals("Italian"))
                    config.SpeechSynthesisLanguage = "it-IT";
                else if (detectedLanguage.Equals("French"))
                    config.SpeechSynthesisLanguage = "fr-FR";
                else if (detectedLanguage.Equals("German"))
                    config.SpeechSynthesisLanguage = "de-DE";
            }

            // Creates a speech synthesizer using the default speaker as audio output.
            using (var synthesizer = new SpeechSynthesizer(config))
            {
                // Receive a text from console input and synthesize it to speaker.
                
                string text = testo;

                using (var result = await synthesizer.SpeakTextAsync(text))
                {
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                    
                }

                // This is to give some time for the speaker to finish playing back the audio
                /*Console.WriteLine("Press any key to exit...");
                Console.ReadKey();*/

            }
        }

        public void setDetectedText(string text)
        {
            TextToTranslate.Text = text;
        }

        public static async Task SynthesisToTextAsync()
        {
            Thread.Sleep(5000);
            var config = SpeechConfig.FromSubscription(COGNITIVE_SERVICES_KEY, "westeurope");
            if (fromLanguage != null && !fromLanguage.Equals("Detect"))
            {
                if (fromLanguage.Equals("Italian"))
                    config.SpeechRecognitionLanguage = "it-IT";
                else if (fromLanguage.Equals("French"))
                    config.SpeechRecognitionLanguage = "fr-FR";
                else if (fromLanguage.Equals("German"))
                    config.SpeechRecognitionLanguage = "de-DE";
            }
            Console.WriteLine("Language chosen: " + fromLanguage);

            using (var audioInput = AudioConfig.FromWavFileInput(@cammino+"system_recorded_audio.wav"))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    Console.WriteLine("Recognizing first result...");
                    var result = await recognizer.RecognizeOnceAsync();

                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"We recognized: {result.Text}");

                        textDetected = result.Text;
                    }
                    else if (result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                }
            }
            File.Delete(@cammino+"system_recorded_audio.wav");
        }
        private async void TranslateButton_Click2(String text)
        {
            

            string fromLanguage = FromLanguageComboBox.SelectedValue.ToString();
            string fromLanguageCode;

            // Auto-detect source language if requested
            if (fromLanguage == "Detect")
            {
                fromLanguageCode = DetectLanguage(text);
                if (!languageCodes.Contains(fromLanguageCode))
                {
                    MessageBox.Show("The source language could not be detected automatically " +
                        "or is not supported for translation.", "Language detection failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
                fromLanguageCode = languageCodesAndTitles[fromLanguage];

            string toLanguageCode = languageCodesAndTitles[ToLanguageComboBox.SelectedValue.ToString()];

            // Spell-check the source text if the source language is English
            if (fromLanguageCode == "en")
            {
                if (text.StartsWith("-"))    // don't spell check in this case
                    text = text.Substring(1);
                else
                {
                    text = CorrectSpelling(text);
                        // put corrected text into input field
                }
            }

            // Handle null operations: no text or same source/target languages
            if (text == "" || fromLanguageCode == toLanguageCode)
            {
                TranslatedTextLabel.Content = text;
                return;
            }

            // Send translation request
            string endpoint = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "translate");
            string uri = string.Format(endpoint + "&from={0}&to={1}", fromLanguageCode, toLanguageCode);

            System.Object[] body = new System.Object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");
                request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody);
                var translation = result[0]["translations"][0]["text"];

                // Update the translation field
                translateDocument( translation);
            }
            detectedLanguage = ToLanguageComboBox.SelectedItem.ToString();
        }
        private void translateDocument(string txt) {
            string docPath =
              Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Write the string array to a new file named "WriteLines.txt".
            String title = "DocumentTradotto.txt";
            int n = 0;
            while(File.Exists(Path.Combine(docPath, title))){
                n++;
                title= "DocumentoTradotto("+n+").txt";
                
            }
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, title)))
            {

                outputFile.Write(txt);
                var str= Encoding.UTF8.GetBytes(txt);
                Console.WriteLine(Encoding.UTF8.GetString(str));
            }
        }
        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opnfd = new OpenFileDialog();
            opnfd.Filter = "Image Files (*.jpg;*.jpeg;.*.gif;)|*.jpg;*.jpeg;.*.gif|"+ "Pdf Files|*.pdf";
            //Pdf Files|*.pdf
            if (opnfd.ShowDialog() != false)
            {
                Console.Out.WriteLine(opnfd.FileName);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri(opnfd.FileName);
                immagine.Source = bitmapImage;

                //TextToTranslate.Text = await MakeRequest2(opnfd.FileName);
                TranslateButton_Click2(await MakeRequest2(opnfd.FileName));
            }
        }

        private void Log(object sender, RoutedEventArgs e)
        {

            var log = new Window1();
            log.Show();
            this.Close();
        }

        private void TextToTranslate_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

            Cronologia.Items.Clear();
            
            /* while (Cronologia.Items.Count > 1)
             {
                 Cronologia.Items.Remove(0);
             }*/


            foreach (String [] a in appoggio)
            {
                Cronologia.MinHeight = Cronologia.Items.Count * 23;

                Console.WriteLine(a[0]+""+a[1]);
                if(a[0].Contains(TextToTranslate.Text))
                if (Cronologia.Items.Count <= 4)
                {
                    Cronologia.Items.Add(a[0]);
                    Cronologia.MinHeight = Cronologia.Items.Count * 23;
                }
            }
            
            
            Console.WriteLine(cache);
            
         UpdateLayout();
            
            // TextToTranslate.text;
        }
     
        private void PrintText(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                Console.WriteLine(Cronologia.SelectedItem.ToString());
                string app = Cronologia.SelectedItem.ToString();
                TextToTranslate.Text = app;
            }
            catch { }
        }

        // Redefine the capturer instance with a new instance of the LoopbackCapture class
        //WasapiLoopbackCapture CaptureInstance = new WasapiLoopbackCapture();

        public void StartRecordAudio()
        {
            Console.WriteLine("Started recording audio at " + DateTime.Now);

            record("open new Type waveaudio Alias recsound", "", 0, 0);
            record("record recsound", "", 0, 0);

            // Define the output wav file of the recorded audio
            //string outputFilePath = @"C:\Users\Fulvio\Desktop\system_recorded_audio.wav"; //Mp3 or Wav

            

            // Redefine the audio writer instance with the given configuration
            //WaveFileWriter RecordedAudioWriter = new WaveFileWriter(outputFilePath, CaptureInstance.WaveFormat);

            // When the capturer receives audio, start writing the buffer into the mentioned file
            /*CaptureInstance.DataAvailable += (s, a) =>
            {
                Console.WriteLine("Receiving audio");
                    // Write buffer into the file of the writer instance
                    RecordedAudioWriter.Write(a.Buffer, 0, a.BytesRecorded);
            };*/

            // Start audio recording !
           // CaptureInstance.StartRecording();

            // When the Capturer Stops, dispose instances of the capturer and writer
            /*CaptureInstance.RecordingStopped += (s, a) =>
            {
                Console.WriteLine("Record is being stopped");
                RecordedAudioWriter.Dispose();
                RecordedAudioWriter = null;
                CaptureInstance.Dispose();
            };*/
        }

        public void StopRecordAudio()
        {
            Console.WriteLine("Stopped recording at " + DateTime.Now);
            Console.WriteLine("Il Path ottenuto è: "+ cammino);
            record("save recsound "+cammino+"//system_recorded_audio.wav", "", 0, 0);
            record("close recsound", "", 0, 0);

            //CaptureInstance.StopRecording();
        }
        public async void Parla(object sender, EventArgs e) {
            fromLanguage=FromLanguageComboBox.SelectedValue.ToString(); ;
                toLanguage= ToLanguageComboBox.SelectedValue.ToString();
            
                    Console.WriteLine(fromLanguage + toLanguage);
                     await TranslateSpeechToSpeech(fromLanguage, toLanguage);
             
        }
        public static async Task TranslateSpeechToSpeech(String from,String to)
        {
            // Creates an instance of a speech translation config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechTranslationConfig.FromSubscription(COGNITIVE_SERVICES_KEY, "westeurope");

            // Sets source and target languages.
            // Replace with the languages of your choice, from list found here: https://aka.ms/speech/sttt-languages
            // string fromLanguage = from;
            // string toLanguage = "en-GB";
            while (toLanguage == null)
            {
                Thread.Sleep(100);
            }
            if (fromLanguage != null && !fromLanguage.Equals("Detect"))
            {
                if (fromLanguage.Equals("Italian"))
                    fromLanguage = "it-IT";
                else if (fromLanguage.Equals("French"))
                    fromLanguage = "fr-FR";
                else if (fromLanguage.Equals("German"))
                    fromLanguage = "de-DE";
                else if (from.Equals("English"))
                    fromLanguage = "en-GB";
                
                if (toLanguage != null && !toLanguage.Equals("Detect"))
                {
                    if (toLanguage.Equals("Italian"))
                        toLanguage = "it-IT";
                    else if (to.Equals("French"))
                        toLanguage = "fr-FR";
                    else if (to.Equals("German"))
                        toLanguage = "de-DE";
                    else if (to.Equals("English"))
                        toLanguage = "en-GB";


                }
                else { return; }
            }
            else { return; }
           
            Console.WriteLine(fromLanguage+ toLanguage);
            config.SpeechRecognitionLanguage = fromLanguage;
            config.AddTargetLanguage(toLanguage);

            // Sets the synthesis output voice name.
            // Replace with the languages of your choice, from list found here: https://aka.ms/speech/tts-languages
            config.VoiceName = "de-DE-Hedda";

            // Creates a translation recognizer using the default microphone audio input device.
            using (var recognizer = new TranslationRecognizer(config))
            {
                // Prepare to handle the synthesized audio data.
                recognizer.Synthesizing += (s, e) =>
                {
                    var audio = e.Result.GetAudio();
                    Console.WriteLine(audio.Length != 0
                        ? $"AUDIO SYNTHESIZED: {audio.Length} byte(s)"
                        : $"AUDIO SYNTHESIZED: {audio.Length} byte(s) (COMPLETE)");
                };

                // Starts translation, and returns after a single utterance is recognized. The end of a
                // single utterance is determined by listening for silence at the end or until a maximum of 15
                // seconds of audio is processed. The task returns the recognized text as well as the translation.
                // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
                // shot recognition like command or query.
                // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                Console.WriteLine("Say something...");
                var result = await recognizer.RecognizeOnceAsync();

                // Checks result.
                if (result.Reason == ResultReason.TranslatedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED '{fromLanguage}': {result.Text}");
                    String txt = "";
                    foreach (var element in result.Translations)
                    {
                     txt=txt+element.Value;
                    }
                    await SynthesisToSpeakerAsync(txt);
                    //Console.WriteLine($"TRANSLATED into '{toLanguage}': {result.Translations[toLanguage]}");
                }
                else if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED '{fromLanguage}': {result.Text} (text could not be translated)");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }
        /*public static string StringToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }
        public static string BinaryToString(string data)
        {
            List<Byte> byteList = new List<Byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }*/
    }
       
    }

        

    
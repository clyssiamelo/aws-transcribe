using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AwsTranscribe
{
    public partial class FrmMain : Form
    {
        private const string AWS_PROFILE_NAME = "AWS Educate profile";
        private const string AWS_S3_INPUT = "ia-transcribe-in";
        private const string AWS_S3_OUTPUT = "ia-transcribe-out";

        private AWSCredentials _awsCredentials;
        private readonly RegionEndpoint _region = RegionEndpoint.USEast1;

        AmazonTranscribeServiceClient _transcribeClient;

        private string _fileName;

        private string _jobInExecutionName;

        // DLL Record voice
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            _fileName = Path.GetTempPath() + $"Record-{DateTime.Now.ToFileTime()}.wav";

            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            mciSendString("record recsound", "", 0, 0);
            btnStop.Enabled = true;
            btnRecord.Enabled = false;
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            mciSendString("save recsound " + _fileName, "", 0, 0);
            mciSendString("close recsound", "", 0, 0);

            btnStop.Enabled = false;
            btnRecord.Enabled = true;


            GetCredentials();

            await UploadToS3(_fileName);

            await ExecuteTranscribe();
        }

        //private void btnOpen_Click(object sender, EventArgs e)
        //{
        //    SoundPlayer soundPlayer = new SoundPlayer(_fileName);
        //    soundPlayer.Play();
        //}

        private async Task<bool> UploadToS3(string file)
        {
            try
            {
                AmazonS3Client s3Client = new AmazonS3Client(_awsCredentials, _region);
                TransferUtility fileTransferUtility = new TransferUtility(s3Client);

                await fileTransferUtility.UploadAsync(file, AWS_S3_INPUT);

                return true;
            }
            catch (AmazonS3Exception ex)
            {
                MessageBox.Show($"Error encountered on server. Message: '{0}' when writing an object {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unknown encountered on server. Message: '{0}' when writing an object {ex.Message}");
                return false;
            }
        }

        private async Task ExecuteTranscribe()
        {
            _transcribeClient = new AmazonTranscribeServiceClient(_awsCredentials, _region);

            var transcriptionJobRequest = new StartTranscriptionJobRequest()
            {
                Media = new Media()
                {
                    MediaFileUri = $"s3://ia-transcribe-in/{Path.GetFileName(_fileName)}"
                },
                OutputBucketName = AWS_S3_OUTPUT,
                LanguageCode = LanguageCode.PtBR,
                MediaFormat = MediaFormat.Wav,
                TranscriptionJobName = $"transcribe-custom-audio-{DateTime.Now.ToFileTime()}"
            };

            StartTranscriptionJobResponse jobResponse = await _transcribeClient.StartTranscriptionJobAsync(transcriptionJobRequest);

            _jobInExecutionName = transcriptionJobRequest.TranscriptionJobName;

            //new Task(() => { VerificarStatusJob(); }).Start();

            new Thread(delegate ()
            {
                VerificarStatusJob();
            }).Start();
        }

        private void GetCredentials()
        {
            var chain = new CredentialProfileStoreChain();

            if (!chain.TryGetAWSCredentials(AWS_PROFILE_NAME, out _awsCredentials))
            {
                MessageBox.Show("Error on get credentials");
            }
        }

        private void VerificarStatusJob()
        {
            if (string.IsNullOrEmpty(_jobInExecutionName))
                return;

            GetTranscriptionJobRequest jobStatus = new GetTranscriptionJobRequest
            {
                TranscriptionJobName = _jobInExecutionName,
            };

            GetTranscriptionJobResponse jobData = _transcribeClient.GetTranscriptionJobAsync(jobStatus).Result;

            int contador = 0;

            while (jobData.TranscriptionJob.TranscriptionJobStatus != TranscriptionJobStatus.COMPLETED)
            {
                Task.Delay(2000);

                jobData = _transcribeClient.GetTranscriptionJobAsync(jobStatus).Result;

                richTextBox1.BeginInvoke(new UpdateTextDelegate(UpdateText), $"Aguardando conclusão do job... Tentativa nº {contador}", false, null);
                contador++;
            }

            string json = JsonConvert.SerializeObject(jobData.TranscriptionJob, Formatting.Indented);

            richTextBox1.BeginInvoke(new UpdateTextDelegate(UpdateText), json, true, jobData.TranscriptionJob);
        }

        private void GetFileS3(string nameFile)
        {
            try
            {
                //AmazonS3Client s3Client = new AmazonS3Client(_awsCredentials, _region);

                //TransferUtility fileTransferUtility = new TransferUtility(s3Client);                

                //string caminhoArquivoBaixado = Path.GetTempPath();

                //fileTransferUtility.Download(@"C:\Users\Matheus\", AWS_S3_OUTPUT, nameFile + ".json");                                                

                //string arquivoLido = File.ReadAllText(caminhoArquivoBaixado + nameFile + ".json");                

                //richTextBox2.Clear();
                //richTextBox2.AppendText(arquivoLido);


                richTextBox2.AppendText("Arquivo não lido");
            }
            catch (AmazonS3Exception ex)
            {
                MessageBox.Show($"Error encountered on server. Message: '{0}' when writing an object {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unknown encountered on server. Message: '{0}' when writing an object {ex.Message}");
            }
        }

        delegate void UpdateTextDelegate(string text, bool finished, TranscriptionJob transcriptionJob);
        private void UpdateText(string text, bool finished, TranscriptionJob transcriptionJob)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Clear();
                richTextBox1.AppendText(text);
            }
            else
            {
                richTextBox1.Clear();
                richTextBox1.AppendText(text);
            }

            if (finished)
            {
                GetFileS3(transcriptionJob.TranscriptionJobName);
            }
        }
    }
}

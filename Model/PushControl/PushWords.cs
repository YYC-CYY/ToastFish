﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using ToastFish.Model.SqliteControl;
using ToastFish.Model.Download;
using ToastFish.Model.Mp3;
using System.Threading;
using System.Speech.Synthesis;

namespace ToastFish.PushControl
{
    class PushWords
    {
        // 当前推送单词的状态
        public static int WORD_CURRENT_STATUS = 0;  // 背单词时候的状态，
        public static int WORD_NUMBER = 10;
        public static string WORD_NUMBER_STRING = "";
        public static int QUESTION_CURRENT_RIGHT_ANSWER = -1;
        public static int QUESTION_CURRENT_STATUS = 0;
        public static Dictionary<string, string> AnswerDict = new Dictionary<string, string> {
            {"0","A"},{"1","B"},{"2","C"},{"3","D"}
        };
        //public static DownloadMp3 Download = new DownloadMp3();

        public static bool IsNumber(string str)
        {
            char[] ch = new char[str.Length];
            ch = str.ToCharArray();
            for (int i = 0; i < ch.Length; i++)    
            {
                if (ch[i] < 48 || ch[i] > 57)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 使用Task防止程序阻塞
        /// </summary>
        public static Task<int> ProcessToastNotificationRecitation()
        {
            var tcs = new TaskCompletionSource<int>();

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                ToastArguments Args = ToastArguments.Parse(toastArgs.Argument);
                string Status = "";
                try
                {
                    Status = Args["action"];
                }
                catch
                {
                }
                if (Status == "succeed")
                {
                    tcs.TrySetResult(0);
                }
                else if (Status == "fail")
                {
                    tcs.TrySetResult(1);
                }
                else if (Status == "UK")
                {
                    tcs.TrySetResult(2);
                }
                else if (Status == "US")
                {
                    tcs.TrySetResult(3);
                }
                else
                {
                    tcs.TrySetResult(1);
                }
            };
            return tcs.Task;
        }

        public static Task<int> ProcessToastNotificationQuestion()
        {
            var tcs = new TaskCompletionSource<int>();

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                ToastArguments Args = ToastArguments.Parse(toastArgs.Argument);
                string Status = "";
                try
                {
                    Status = Args["action"];
                }
                catch
                {
                    tcs.TrySetResult(-1);
                }
                string temp = QUESTION_CURRENT_RIGHT_ANSWER.ToString();
                if (Status == QUESTION_CURRENT_RIGHT_ANSWER.ToString())
                {
                    tcs.TrySetResult(1);
                }
                else
                {
                    tcs.TrySetResult(0);
                }
            };
            return tcs.Task;
        }

        public static Task<int> ProcessToastNotificationSetNumber()
        {
            var tcs = new TaskCompletionSource<int>();

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                ToastArguments Args = ToastArguments.Parse(toastArgs.Argument);
                string Status = "";
                try
                {
                    Status = Args["action"];
                }
                catch
                {
                    tcs.TrySetResult(0);
                }
                if (Status == "yes")
                {
                    WORD_NUMBER_STRING = (string)toastArgs.UserInput["number"];
                    tcs.TrySetResult(1);
                }
                else
                {
                    tcs.TrySetResult(0);
                }
            };
            return tcs.Task;
        }

        //public static Task<int> ProcessToastNotificationSpeech(string Speech, string headWord)
        //{
        //    var tcs = new TaskCompletionSource<int>();

        //    if (SPEECH_STATUS == 0)
        //    {
        //        tcs.TrySetResult(0);
        //    }
        //    else if (SPEECH_STATUS == 1)
        //    {
        //        tcs.TrySetResult(1);
        //    }
        //    else if (SPEECH_STATUS == 2)
        //    {
        //        tcs.TrySetResult(2);
        //    }
        //    return tcs.Task;
        //}

        public static void SetWordNumber()
        {
            new ToastContentBuilder()
            .AddText("这次要背多少个？")
            .AddToastInput(new ToastSelectionBox("number")
            {
                DefaultSelectionBoxItemId = "10",
                Items =
                {
                    new ToastSelectionBoxItem("5", "5"),
                    new ToastSelectionBoxItem("10", "10"),
                    new ToastSelectionBoxItem("15", "15"),
                    new ToastSelectionBoxItem("20", "20")
                }
            })
            .AddButton(new ToastButton()
                .SetContent("确定")
                .AddArgument("action", "yes")
                .SetBackgroundActivation())
            .Show();
            var task = PushWords.ProcessToastNotificationSetNumber();
            if(task.Result == 1)
            {
                if(IsNumber(WORD_NUMBER_STRING))
                {
                    WORD_NUMBER = int.Parse(WORD_NUMBER_STRING);
                    PushMessage("已设置单词数量为：" + WORD_NUMBER_STRING);
                }
            } 
        }

        public static void Recitation(Object Number)
        {
            Select Query = new Select();
            List<Word> RandomList = Query.GetRandomWordList((int)Number);
            List<Word> CopyList = Clone<Word>(RandomList);
            while (CopyList.Count != 0)
            {
                Word CurrentWord = Query.GetRandomWord(CopyList);
                PushOneWord(CurrentWord);

                WORD_CURRENT_STATUS = 2;
                while (WORD_CURRENT_STATUS == 2)
                {
                    var task = PushWords.ProcessToastNotificationRecitation();
                    if (task.Result == 0)
                    {
                        WORD_CURRENT_STATUS = 1;
                    }
                    else if (task.Result == 1)
                    {
                        WORD_CURRENT_STATUS = 0;
                    }
                    else if (task.Result == 2)
                    {
                        WORD_CURRENT_STATUS = 0;
                        SpeechSynthesizer synth = new SpeechSynthesizer();
                        synth.SpeakAsync(CurrentWord.headWord);

                        //Download.HttpDownload("https://dict.youdao.com/dictvoice?audio=" + CurrentWord.usSpeech + ".mp3", CurrentWord.headWord + "_2");
                        //MUSIC MIC = new MUSIC();
                        //MIC.FileName = System.IO.Directory.GetCurrentDirectory() + @"\Mp3Cache\" + CurrentWord.headWord + "_2.mp3";
                        //MIC.play();
                    }
                    //else if (task.Result == 3)
                    //{
                    //    WORD_CURRENT_STATUS = 0;
                    //    Download.HttpDownload("https://dict.youdao.com/dictvoice?audio=" + CurrentWord.usSpeech + ".mp3", CurrentWord.headWord + "_1");
                    //    MUSIC MIC = new MUSIC();
                    //    MIC.FileName = System.IO.Directory.GetCurrentDirectory() + @"\Mp3Cache\" + CurrentWord.headWord + "_1.mp3";
                    //    MIC.play();
                    //}
                }
                if (WORD_CURRENT_STATUS == 1)
                {
                    Query.UpdateWord(CurrentWord.wordRank);
                    Query.UpdateCount();
                    CopyList.Remove(CurrentWord);
                }
            }
            PushMessage("背完了！接下来开始测验！");
            // 背诵结束

            CopyList = Clone<Word>(RandomList);

            for (int i = CopyList.Count - 1; i >= 0; i--)
            {
                if (CopyList[i].question != null)
                    CopyList.Remove(CopyList[i]);
            }

            while (CopyList.Count != 0)
            {
                ToastNotificationManagerCompat.History.Clear();
                Word CurrentWord = Query.GetRandomWord(CopyList);
                List<Word> FakeWordList = Query.GetRandomWordList(2);

                PushOneTransQuestion(CurrentWord, FakeWordList[0].headWord, FakeWordList[1].headWord);

                QUESTION_CURRENT_STATUS = 2;
                while (QUESTION_CURRENT_STATUS == 2)
                {
                    var task = PushControl.PushWords.ProcessToastNotificationQuestion();
                    if (task.Result == 1)
                        QUESTION_CURRENT_STATUS = 1;
                    else if(task.Result == 0)
                        QUESTION_CURRENT_STATUS = 0;
                }
                if (QUESTION_CURRENT_STATUS == 1)
                {
                    CopyList.Remove(CurrentWord);
                    PushMessage("正确,太强了吧！");
                    Thread.Sleep(3000);
                }
                else if (QUESTION_CURRENT_STATUS == 0)
                {
                    //CopyList.Remove(CurrentWord);
                    new ToastContentBuilder()
                    .AddText("错误 正确答案：" + AnswerDict[QUESTION_CURRENT_RIGHT_ANSWER.ToString()] + '.' + CurrentWord.headWord)
                    .Show();
                    Thread.Sleep(3000);
                }
            }

            for (int i = RandomList.Count - 1; i >= 0; i--)
            {
                if (RandomList[i].question == null)
                    RandomList.Remove(RandomList[i]);
            }

            while (RandomList.Count != 0)
            {
                ToastNotificationManagerCompat.History.Clear();
                Word CurrentWord = Query.GetRandomWord(RandomList);
                PushOneQuestion(CurrentWord);

                QUESTION_CURRENT_RIGHT_ANSWER = int.Parse(CurrentWord.rightIndex) - 1;
                QUESTION_CURRENT_STATUS = 2;
                while (QUESTION_CURRENT_STATUS == 2)
                {
                    var task = ProcessToastNotificationQuestion();
                    if (task.Result == 1)
                        QUESTION_CURRENT_STATUS = 1;
                    else if (task.Result == 0)
                        QUESTION_CURRENT_STATUS = 0;
                }
                if (QUESTION_CURRENT_STATUS == 1)
                {
                    RandomList.Remove(CurrentWord);
                    PushMessage("正确,太强了吧！");
                    Thread.Sleep(3000);
                }
                else if (QUESTION_CURRENT_STATUS == 0)
                {
                    RandomList.Remove(CurrentWord);
                    new ToastContentBuilder()
                    .AddText("错误, 正确答案：" + AnswerDict[QUESTION_CURRENT_RIGHT_ANSWER.ToString()])
                    .AddText(CurrentWord.explain)
                    .Show();
                    Thread.Sleep(8000);
                }
            }
            
            PushMessage("结束了！恭喜！");
        }

        public static void PushMessage(string Message, string Buttom = "")
        {
            if (Buttom != "")
                new ToastContentBuilder()
                .AddText("Toast Fish")
                .AddText(Message)
                .AddButton(new ToastButton()
                .SetContent(Buttom)
                .AddArgument("action", "succeed")
                .SetBackgroundActivation())
                .Show();
            else
                new ToastContentBuilder()
                .AddText("Toast Fish")
                .AddText(Message)
                .Show();
        }

        public static void PushOneWord(Word CurrentWord)
        {
            ToastNotificationManagerCompat.History.Clear();
            string WordPhonePosTran = CurrentWord.headWord + "  (" + CurrentWord.usPhone + ")\n" + CurrentWord.pos + ". " + CurrentWord.tranCN;
            string SentenceTran = "";
            if(CurrentWord.sentence != null && CurrentWord.sentence.Length < 50)
            {
                SentenceTran = CurrentWord.sentence + '\n' + CurrentWord.sentenceCN;
            }
            else if(CurrentWord.phrase != null)
            {
                SentenceTran = CurrentWord.phrase + '\n' + CurrentWord.phraseCN;
            }
            new ToastContentBuilder()
            .AddText(WordPhonePosTran)
            .AddText(SentenceTran)
            
            .AddButton(new ToastButton()
                .SetContent("记住了！")
                .AddArgument("action", "succeed")
                .SetBackgroundActivation())

            .AddButton(new ToastButton()
                .SetContent("暂时跳过..")
                .AddArgument("action", "fail")
                .SetBackgroundActivation())
            
            .AddButton(new ToastButton()
                .SetContent("发音")
                .AddArgument("action", "UK")
                .SetBackgroundActivation())

            //.AddButton(new ToastButton()
            //    .SetContent("美音")
            //    .AddArgument("action", "US")
            //    .SetBackgroundActivation())
            .Show();
        }

        public static void PushOneQuestion(Word CurrentWord)
        {
            string Question = CurrentWord.question;
            string A = CurrentWord.choiceIndexOne;
            string B = CurrentWord.choiceIndexTwo;
            string C = CurrentWord.choiceIndexThree;
            string D = CurrentWord.choiceIndexFour;

            new ToastContentBuilder()
            .AddText("Question")
            .AddText(Question)
            
            .AddButton(new ToastButton()
                .SetContent(A)
                .AddArgument("action", "1")
                .SetBackgroundActivation())

            .AddButton(new ToastButton()
                .SetContent(B)
                .AddArgument("action", "2")
                .SetBackgroundActivation())
            
            .AddButton(new ToastButton()
                .SetContent(C)
                .AddArgument("action", "3")
                .SetBackgroundActivation())

            .AddButton(new ToastButton()
                .SetContent(D)
                .AddArgument("action", "4")
                .SetBackgroundActivation())
            .Show();

        }

        public static void PushOneTransQuestion(Word CurrentWord, string B, string C)
        {
            string Question = CurrentWord.tranCN;
            string A = CurrentWord.headWord;

            Random Rd = new Random();
            int AnswerIndex = Rd.Next(3);
            QUESTION_CURRENT_RIGHT_ANSWER = AnswerIndex;

            if (AnswerIndex == 0)
            {
                new ToastContentBuilder()
               .AddText("翻译")
               .AddText(Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + A)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + B)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + C)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())

               .Show();
            }
            else if (AnswerIndex == 1)
            {
               new ToastContentBuilder()
              .AddText("Question")
              .AddText(Question)

              .AddButton(new ToastButton()
                  .SetContent("A." + B)
                  .AddArgument("action", "0")
                  .SetBackgroundActivation())

              .AddButton(new ToastButton()
                  .SetContent("B." + A)
                  .AddArgument("action", "1")
                  .SetBackgroundActivation())

              .AddButton(new ToastButton()
                  .SetContent("C." + C)
                  .AddArgument("action", "2")
                  .SetBackgroundActivation())
              .Show();
            }
            else if (AnswerIndex == 2)
            {
               new ToastContentBuilder()
              .AddText("Question")
              .AddText(Question)

              .AddButton(new ToastButton()
                  .SetContent("A." + C)
                  .AddArgument("action", "0")
                  .SetBackgroundActivation())

              .AddButton(new ToastButton()
                  .SetContent("B." + B)
                  .AddArgument("action", "1")
                  .SetBackgroundActivation())

              .AddButton(new ToastButton()
                  .SetContent("C." + A)
                  .AddArgument("action", "2")
                  .SetBackgroundActivation())
              .Show();
            }
        }

        /// <summary>
        /// 克隆Word列表
        /// </summary>
        /// <typeparam name="Word"></typeparam>
        /// <param name="RealObject"></param>
        public static List<Word> Clone<Word>(List<Word> RealObject)
        {
            using (Stream objStream = new MemoryStream())
            {
                //利用 System.Runtime.Serialization序列化与反序列化完成引用对象的复制
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objStream, RealObject);
                objStream.Seek(0, SeekOrigin.Begin);
                return (List<Word>)formatter.Deserialize(objStream);
            }

        }
    }
}

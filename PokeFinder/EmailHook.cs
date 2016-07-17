using System;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PokeFinder
{
    class EmailHook
    {
        string replyEmail;
        string message;

        public EmailHook(string r, string m)
        {
            replyEmail = r;
            message = m;
        }
        
        public bool SendMessage(Context c)
        {
            MailMessage mMessage = new MailMessage();
            mMessage.To.Add("millbj92@gmail.com");
            mMessage.Subject = "New Bug Report";
            mMessage.From = new MailAddress(replyEmail);
            mMessage.Body = message;
            SmtpClient smtp = new SmtpClient("mail.pokefindergo.com", 587);
            smtp.Credentials = new NetworkCredential("support@pokefindergo.com", "eq9935sm");
            try
            {
                smtp.Send(mMessage);
            }
            catch(SmtpException ex)
            {
                Toast.MakeText(c, ex.Message, ToastLength.Short).Show();
                return false;
            }
            return true;
        }
    }
}
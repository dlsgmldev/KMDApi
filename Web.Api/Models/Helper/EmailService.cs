﻿using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KDMApi.Models.Helper
{
	public class EmailService : IEmailService
	{
		private readonly IEmailConfiguration _emailConfiguration;

		public EmailService(IEmailConfiguration emailConfiguration)
		{
			_emailConfiguration = emailConfiguration;
		}

		public List<EmailMessage> ReceiveEmail(int maxCount = 10)
		{
			throw new NotImplementedException();
		}

		public void Send(EmailMessage emailMessage)
		{
			var message = new MimeMessage();
			message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
			message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));

			message.Subject = emailMessage.Subject;
			//We will say we are sending HTML. But there are options for plaintext etc. 
			message.Body = new TextPart(TextFormat.Html)
			{
				Text = emailMessage.Content
			};

			//Be careful that the SmtpClient class is the one from Mailkit not the framework!
			using (var emailClient = new SmtpClient())
			{
				//The last parameter here is to use SSL (Which you should!)
				emailClient.Connect(_emailConfiguration.SmtpServer, _emailConfiguration.SmtpPort, true);

				//Remove any OAuth functionality as we won't be using it. 
				emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

				emailClient.Authenticate(_emailConfiguration.SmtpUsername, _emailConfiguration.SmtpPassword);

				emailClient.Send(message);

				emailClient.Disconnect(true);
			}

			/*
			 * using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(server, port);
                    await client.AuthenticateAsync(sender, password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
			 */
		}
	}
}

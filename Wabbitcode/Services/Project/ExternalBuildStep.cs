﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using Revsoft.Wabbitcode.Interface;
using Revsoft.Wabbitcode.Utilities;

namespace Revsoft.Wabbitcode.Services.Project
{
	public class ExternalBuildStep : IBuildStep
	{
		public IProject Project { get; private set; }

		public string Arguments { get; private set; }

		public FilePath InputFile { get; set; }

		public int StepNumber { get; set; }

		public Action<string> Callback { get; set; }

		public ExternalBuildStep(int number, string commandLine)
		{
			StepNumber = number;
			int spaceIndex = commandLine.IndexOf(' ');
			if (spaceIndex == -1)
			{
				InputFile = new FilePath(commandLine);
				Arguments = "";
			}
			else
			{
				InputFile = new FilePath(commandLine.Substring(0, spaceIndex));
				Arguments = commandLine.Substring(spaceIndex, commandLine.Length - spaceIndex);
			}

		}

		public ExternalBuildStep(int number, string program, string arguments)
		{
			StepNumber = number;
			InputFile = new FilePath(program);
			Arguments = arguments;
		}

		[SecurityPermission(SecurityAction.LinkDemand)]
		public bool Build()
		{
			Process cmd = new Process
			{
				StartInfo =
				{
					FileName = InputFile,
					WorkingDirectory = Project.ProjectDirectory,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					Arguments = Arguments,
				}
			};
			cmd.Exited += externalProgramExit;
			try
			{
				cmd.Start();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void externalProgramExit(object sender, EventArgs e)
		{
			Process cmd = (Process)sender;
			Callback(cmd.StandardOutput.ReadToEnd());
		}


		public string Description
		{
			get { return "Run " + Path.GetFileName(InputFile); }
		}

		public override string ToString()
		{
			return Description;
		}

		public object Clone()
		{
			return new ExternalBuildStep(StepNumber, InputFile, Arguments);
		}
	}
}
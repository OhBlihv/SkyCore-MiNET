using System.Collections.Generic;
using MiNET;
using MiNET.Worlds;
using SkyCore.Player;

namespace SkyCore.Util
{

	public class BarContent
	{

		public string Content { get; }

		public int ExpiryTicks { get; set; }

		public BarContent(string barContent, int expiryTicks)
		{
			Content = barContent;
			ExpiryTicks = expiryTicks;
		}

	}

	public class BarHandler
	{

		private readonly SortedDictionary<int, List<BarContent>> _popupContentQueue = new SortedDictionary<int, List<BarContent>>();
		private readonly SortedDictionary<int, List<BarContent>> _barContentQueue   = new SortedDictionary<int, List<BarContent>>();

		private readonly SkyPlayer _player;

		public BarHandler(SkyPlayer player)
		{
			_player = player;
		}

		public void AddMinorLine(string line, int expiryTicks = 2, int priority = 5)
		{
			AddToPriorityQueue(_barContentQueue, priority, line, expiryTicks);
		}

		public void AddMajorLine(string line, int expiryTicks = 4, int priority = 5)
		{
			AddToPriorityQueue(_popupContentQueue, priority, line, expiryTicks);
		}

		private void AddToPriorityQueue(SortedDictionary<int, List<BarContent>> priortyMap, int priority, string line, int expiryTicks)
		{
			BarContent barContent = new BarContent(line, expiryTicks);

			List<BarContent> contentQueue;
			if (priortyMap.ContainsKey(priority))
			{
				contentQueue = priortyMap[priority];
			}
			else
			{
				contentQueue = new List<BarContent>();
				priortyMap.Add(priority, contentQueue);
			}

			contentQueue.Add(barContent);
		}

		public void Clear()
		{
			_popupContentQueue.Clear();
			_barContentQueue.Clear();
		}

		public void DoTick()
		{
			List<int> removedKeys = new List<int>();
			bool isContentAlreadyVisible = false;
			foreach (int priorityKey in _popupContentQueue.Keys)
			{
				string visibleContent = ProcessContentQueue(_popupContentQueue[priorityKey]);
				if (visibleContent == null)
				{
					removedKeys.Add(priorityKey);
				}
				else if(!isContentAlreadyVisible)
				{
					string content;
					if (_player.GameMode == GameMode.Creative)
					{
						content = $"{visibleContent}";
					}
					else
					{
						content = $"{visibleContent}";
					}

					_player.SendMessage(content, MessageType.Popup);
					isContentAlreadyVisible = true;
				}
			}

			foreach (int priorityKey in removedKeys)
			{
				_popupContentQueue.Remove(priorityKey);
			}

			removedKeys.Clear();

			isContentAlreadyVisible = false;

			foreach (int priorityKey in _barContentQueue.Keys)
			{
				string visibleContent = ProcessContentQueue(_barContentQueue[priorityKey]);
				if (visibleContent == null)
				{
					removedKeys.Add(priorityKey);
				}
				else if(!isContentAlreadyVisible)
				{
					_player.SendTitle("", TitleType.AnimationTimes, 6, 6, 20);

					string content;
					if (_player.GameMode == GameMode.Creative)
					{
						content = $"§f\n§f\n§f\n{visibleContent}";
					}
					else
					{
						content = $"§f\n{visibleContent}\n§f\n§f";
					}

					_player.SendTitle(content, TitleType.ActionBar);
					isContentAlreadyVisible = true;
				}
			}

			foreach (int priorityKey in removedKeys)
			{
				_barContentQueue.Remove(priorityKey);
			}
		}
	
		//<return> Returns message to print, otherwise indicates the queue is empty and can be removed
		private string ProcessContentQueue(List<BarContent> contentList)
		{
			List<BarContent> removedContents = new List<BarContent>();

			string visibleContent = null;
			foreach (BarContent barContent in contentList)
			{
				if (visibleContent == null)
				{
					visibleContent = barContent.Content;
				}

				//Continue to reduce remaining ticks on existing/non visible messages
				if (--barContent.ExpiryTicks <= 0)
				{
					removedContents.Add(barContent);
				}
			}

			foreach (BarContent barContent in removedContents)
			{
				contentList.Remove(barContent);
			}

			return visibleContent;
		}

	}
}

using System.Collections;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// A throwaway object that times one scripted exchange of the chat app (moved from DreamOS 2026-10-19): wait,
    /// show the typing bubble, wait again, post the message; then it removes itself. The chat app makes one per
    /// dynamic reply and per storyteller item.
    /// </summary>
    public class DynamicMessageHandler : MonoBehaviour
    {
        [HideInInspector] public MessagingApp manager;
        GameObject typingBubble;

        MessagingChat Chat(int layoutIndex) => manager.chatList[layoutIndex].chatAsset;
        MessagingChat.StoryTeller Item(int layoutIndex) => Chat(layoutIndex).storyTeller[manager.storyTellerIndex];
        ChatLayoutPreset Layout(int layoutIndex) => manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();

        void ShowTyping(int layoutIndex)
        {
            typingBubble = Instantiate(manager.chatMessageTimer, Vector3.zero, Quaternion.identity);
            typingBubble.transform.SetParent(Layout(layoutIndex).messageParent, false);
        }

        /// <summary>The contact answers a dynamic message; typing is blocked until it has.</summary>
        public IEnumerator HandleDynamicMessage(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);
            ShowTyping(layoutIndex);
            StartCoroutine(FinishDynamicMessage(Chat(layoutIndex).dynamicMessages[manager.dynamicMessageIndex].replyTimer, layoutIndex));
        }

        IEnumerator FinishDynamicMessage(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            manager.allowInputSubmit = true;
            manager.CreateDynamicMessage(layoutIndex, false);

            var dynamic = Chat(layoutIndex).dynamicMessages[manager.dynamicMessageIndex];
            if (dynamic.replyBehavior == MessagingChat.DynamicMessageReplyBehavior.DisableReply) dynamic.enableReply = false;

            Destroy(typingBubble);
            Destroy(gameObject);
        }

        /// <summary>The storyteller item's own line, then its reply choices come up.</summary>
        public IEnumerator HandleStoryTeller(float timer, int layoutIndex, bool isIndividual)
        {
            yield return new WaitForSeconds(timer);
            ShowTyping(layoutIndex);
            StartCoroutine(CreateStoryTellerMessage(Item(layoutIndex).messageTimer, layoutIndex, isIndividual));
        }

        IEnumerator CreateStoryTellerMessage(float timer, int layoutIndex, bool isIndividual)
        {
            yield return new WaitForSeconds(timer);
            Destroy(typingBubble);

            var item = Item(layoutIndex);
            if (isIndividual) manager.CreateCustomIndividualMessage(Layout(layoutIndex), item.messageContent, manager.GetTimeData(), item.messageKey);
            else manager.CreateCustomMessage(Layout(layoutIndex), item.messageContent, manager.GetTimeData(), item.messageKey);

            if (manager.stIndexHelper == manager.currentLayout && manager.storyTellerAnimator.gameObject.activeInHierarchy) manager.ShowStorytellerPanel();
            manager.isStoryTellerOpen = true;
        }

        /// <summary>After a reply is picked: the contact types, answers, and the reply's follow-up item runs.</summary>
        public IEnumerator HandleStoryTellerLatency(float timer, int layoutIndex, int itemIndex)
        {
            yield return new WaitForSeconds(timer);
            StartCoroutine(FinishStoryTeller(Item(layoutIndex).replies[itemIndex].feedbackTimer, layoutIndex));
            ShowTyping(layoutIndex);
        }

        IEnumerator FinishStoryTeller(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            var reply = Item(layoutIndex).replies[manager.stItemIndex];
            manager.CreateIndividualMessage(Layout(layoutIndex), reply.replyFeedback, reply.feedbackKey);
            if (!string.IsNullOrEmpty(reply.callAfter)) manager.CreateStoryTeller(manager.chatList[layoutIndex].chatTitle, reply.callAfter);

            Destroy(typingBubble);
            Destroy(gameObject);
        }
    }
}

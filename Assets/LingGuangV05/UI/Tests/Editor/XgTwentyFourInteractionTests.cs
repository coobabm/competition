using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace LingGuangV05.UI.Tests
{
    public sealed class XgTwentyFourInteractionTests
    {
        GameObject root;object game,context;Type type;int reports;bool active;
        object Read(string key)=>type.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
        void Write(string key,object value)=>type.GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
        void Call(string name)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        Button Button(string name)=>root.transform.Find(name).GetComponent<Button>();
        void Click(string name){var button=Button(name);Assert.IsTrue(button.interactable,name+" is enabled");button.onClick.Invoke();}
        List<double> Values=>(List<double>)Read("values");
        [SetUp]public void SetUp()
        {
            root=new GameObject("24 test",typeof(RectTransform),typeof(Canvas));active=true;reports=0;
            var sim=Activator.CreateInstance(Type.GetType("LingGuangV05.XingGuang.XgSim, LingGuangV05.XingGuang.Core",true),new object[]{null});
            var uiType=Type.GetType("LingGuangV05.Desktop.XingGuang.XgUi, Assembly-CSharp",true);
            var fontType=Type.GetType("LingGuangV05.Desktop.XingGuang.XgUiV4Fonts, Assembly-CSharp",true);
            var font=fontType.GetProperty("Sans",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var ui=Activator.CreateInstance(uiType,new object[]{font,null,true});
            var contextType=Type.GetType("LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniContext, Assembly-CSharp",true);
            context=Activator.CreateInstance(contextType,new object[]{(RectTransform)root.transform,ui,sim,null,"make24",(Func<bool>)(()=>active),(Func<double>)(()=>0),(Action<string>)(_=>{}),(Action<bool,double>)((_,__)=>reports++)});
            type=Type.GetType("LingGuangV05.Desktop.XingGuang.MiniGames.XgTwentyFourPack, Assembly-CSharp",true);
            game=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{context},null);type.GetMethod("Build").Invoke(game,null);
            SetCards(1,2,3,4);
        }
        void SetCards(params double[] values)
        {
            Values.Clear();Values.AddRange(values);var expr=(List<string>)Read("expressions");expr.Clear();foreach(var v in values)expr.Add(v.ToString());
            var originals=(List<double>)Read("original");originals.Clear();originals.AddRange(values);
            Write("building",true);Write("a",-1);Write("b",-1);Call("Paint");
        }
        [TearDown]public void TearDown(){if(context!=null)context.GetType().GetMethod("Close").Invoke(context,null);(game as IDisposable)?.Dispose();if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
        [Test]public void CombineUndoResetAndSubmitUseActualButtons()
        {
            Assert.IsFalse(Button("Op0").interactable);Assert.IsFalse(Button("SubmitExpression").interactable);
            Click("Number0");Click("Number1");Click("Op0");CollectionAssert.AreEqual(new double[]{3,4,3},Values);
            Click("Undo");CollectionAssert.AreEqual(new double[]{1,2,3,4},Values);
            Click("Number0");Click("Number1");Click("ClearSelection");Assert.AreEqual(-1,Read("a"));Assert.AreEqual(-1,Read("b"));
            Click("Number0");Click("Number1");Click("Op0");Click("RestartExpression");CollectionAssert.AreEqual(new double[]{1,2,3,4},Values);
            // (1 + 3) * (2 + 4) = 24, using each original card exactly once.
            Click("Number0");Click("Number2");Click("Op0");Click("Number0");Click("Number1");Click("Op0");Click("Number0");Click("Number1");Click("Op2");
            Assert.AreEqual(24,Values[0]);Assert.AreEqual(0,reports,"expression waits for explicit submission");
            Click("SubmitExpression");Click("SubmitExpression");Assert.AreEqual(1,reports);
        }
        [Test]public void ZeroDivisionHasLocalFeedbackWithoutConsumingCards()
        {
            SetCards(0,2,3,4);Click("Number1");Click("Number0");Click("Op3");
            Assert.AreEqual(4,Values.Count);Assert.IsNotEmpty((string)Read("message"));Assert.IsFalse(Button("Undo").interactable);Assert.AreEqual(0,reports);
            Click("Swap");Click("Op3");CollectionAssert.AreEqual(new double[]{3,4,0},Values);
        }
        [Test]public void DropRequiresActualSlotBoundsAndClosedContextsIgnoreButtons()
        {
            var method=type.GetMethod("DropSlot",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.AreEqual(0,method.Invoke(null,new object[]{new Vector2(90,200)}));Assert.AreEqual(1,method.Invoke(null,new object[]{new Vector2(800,240)}));
            foreach(var p in new[]{new Vector2(450,210),new Vector2(100,100),new Vector2(800,300)})Assert.AreEqual(-1,method.Invoke(null,new object[]{p}));
            active=false;Button("Number0").onClick.Invoke();Assert.AreEqual(-1,Read("a"));
            active=true;context.GetType().GetMethod("Close").Invoke(context,null);Button("Number1").onClick.Invoke();Assert.AreEqual(-1,Read("a"));Assert.AreEqual(0,reports);
        }
        [TestCase(1f)] [TestCase(.65f)]
        public void ActualPointerDragFollowsCursorAndOnlyAcceptsSlotDrops(float scale)
        {
            var surface=(RectTransform)root.transform;
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            surface.sizeDelta=new Vector2(900,480);surface.localScale=Vector3.one*scale;
            surface.position=new Vector3(500,350,0);
            var pointerType=Type.GetType("LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniPointer, Assembly-CSharp",true);
            Vector2 ScreenAt(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(null,
                surface.TransformPoint(new Vector3(surface.rect.xMin+point.x,surface.rect.yMax-point.y,0)));
            void Drag(int index,Vector2 destination)
            {
                var card=Button("Number"+index);
                var pointer=card.GetComponent(pointerType);
                var events=new PointerEventData(null){button=PointerEventData.InputButton.Left,
                    position=ScreenAt(new Vector2(45+index*210+95,137.5f)),pointerDrag=card.gameObject};
                ((IPointerDownHandler)pointer).OnPointerDown(events);
                events.delta=ScreenAt(destination)-events.position;events.position=ScreenAt(destination);events.dragging=true;
                ((IDragHandler)pointer).OnDrag(events);
                var ghost=(RectTransform)surface.Find("DragCard");Assert.IsNotNull(ghost,"actual drag creates feedback");
                Vector2 ghostCentre=RectTransformUtility.WorldToScreenPoint(null,ghost.TransformPoint(ghost.rect.center));
                Assert.Less(Vector2.Distance(events.position,ghostCentre),.1f,"drag ghost centre follows cursor at current canvas scale");
                var release=(IPointerUpHandler)pointer;release.OnPointerUp(events);
                Assert.IsNull(surface.Find("DragCard"),"release removes drag feedback and rebuilds original card positions");
                int selectedA=(int)Read("a"),selectedB=(int)Read("b");
                release.OnPointerUp(events); // Stale duplicate release must never reselect or settle.
                Assert.AreEqual(selectedA,Read("a"));Assert.AreEqual(selectedB,Read("b"));Assert.AreEqual(0,reports);
            }
            Drag(0,new Vector2(160,220));Assert.AreEqual(0,Read("a"));Assert.AreEqual(-1,Read("b"));
            Drag(1,new Vector2(650,220));Assert.AreEqual(0,Read("a"));Assert.AreEqual(1,Read("b"));
            Drag(2,new Vector2(450,220)); // Gap between A/B is not a drop slot.
            Assert.AreEqual(0,Read("a"));Assert.AreEqual(1,Read("b"));Assert.IsNotEmpty((string)Read("message"));
            CollectionAssert.AreEqual(new double[]{1,2,3,4},Values,"dragging never consumes original numbers or submits an answer");
        }
    }
}

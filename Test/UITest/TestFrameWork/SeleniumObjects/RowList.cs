﻿using UITest.TestDriver;

namespace UITest.SeleniumObjects
{
    public class RowList : UIElement
    {
        public RowList(IDriver Driver, Enums.FINDBY LocatorType, string Locator) : base(Driver)
        {
            base.Locator = Locator;
            base.LocatorType = LocatorType;
        }

        public bool EnterText(string Text)
        {
            FindElement(base.LocatorType, base.Locator);
            SendKeys(Text);
            return (true);
        }

        public string GetText()
        {
            return (Text);
        }
    }
}

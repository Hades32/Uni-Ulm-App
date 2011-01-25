using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace UniUlmApp
{


    public class Mensaplan
    {
        public IList<Tag> Tage { get; private set; }
        public bool HasErrors { get; set; }

        public Action<Mensaplan> _Loaded;
        public event Action<Mensaplan> Loaded
        {
            add
            {
                this._Loaded += value;
                if (this.isLoaded)
                    value(this);
            }
            remove
            {
                this._Loaded -= value;
            }
        }

        public event Action<Mensaplan> OnError;

        private System.IO.Stream cacheStream;
        private bool isLoaded = false;
        private string url;

        public Mensaplan(string url, System.IO.Stream stream)
        {
            this.Loaded += (_) => { };
            this.OnError += (_) => { };
            this.cacheStream = stream;
            this.HasErrors = false;
            this.url = url;
            this.Tage = new List<Tag>();
            if (stream != null && stream.Length > 0)
            {
                this.parseXmlStream(stream, true);
            }
            else
            {
                loadUrl();
            }
        }

        private void loadUrl()
        {
            var web = new WebClient();
            web.OpenReadCompleted += new OpenReadCompletedEventHandler(web_OpenReadCompleted);
            web.OpenReadAsync(new Uri(this.url));
        }

        void web_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    var xmlstream = e.Result;
                    parseXmlStream(xmlstream, false);
                }
                else
                {
                    this.OnError(this);
                }
            }
            catch
            {
                this.OnError(this);
            }
        }

        private void parseXmlStream(System.IO.Stream xmlstream, bool isCached)
        {
            try
            {
                var plan = XDocument.Load(xmlstream);

                // if this call is for a cached stream first check if it is out of date
                // if it isn't from the current week reload it
                if (isCached)
                {
                    // It's a German Mensaplan :)
                    var culture = new System.Globalization.CultureInfo("de-de");
                    var calendarweek = plan.Root.Element("week").Attribute("weekOfYear").Value;
                    var curweek = culture.Calendar.GetWeekOfYear(
                                        DateTime.Now,
                                        culture.DateTimeFormat.CalendarWeekRule,
                                        culture.DateTimeFormat.FirstDayOfWeek);
                    if (calendarweek != curweek.ToString())
                    {
                        this.loadUrl();
                        return;
                    }
                }

                var tage = plan.Root.Elements("week").Elements("day");

                foreach (var tag in tage)
                {
                    var date = DateTime.Parse(tag.Attribute("date").Value);

                    if (tag.Attribute("open").Value == "1")
                    {
                        this.Tage.Add(new Tag()
                        {
                            Date = date,
                            Essen = tag.Elements("meal")
                                .Select(x => new Essen() { Typ = x.Attribute("type").Value, Name = x.Value })
                        });
                    }
                    else
                        this.Tage.Add(new Tag() { Date = date, Essen = generateNoEssenList() });
                }

                if (this.Tage.Count == 0)
                {
                    this.Tage.Add(new Tag() { Date = DateTime.Now, Essen = generateNoEssenList() });
                }
                else if (this.cacheStream != null)
                {
                    this.cacheStream.SetLength(0);
                    plan.Save(this.cacheStream);
                    this.cacheStream.Close();
                    this.cacheStream = null;
                }

                this.HasErrors = false;
                this.isLoaded = true;
                this._Loaded(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                this.HasErrors = true;
                if (isCached)
                {
                    this.cacheStream.SetLength(0);
                    this.loadUrl();
                }
            }
        }

        private IEnumerable<Essen> generateNoEssenList()
        {
            return new List<Essen>() { new Essen() { Typ = "Heute ist leider", Name = "Geschlossen" } };
        }

        public class Tag : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public Tag()
            {
                //avoid null checks
                this.PropertyChanged += (_, __) => { };
            }

            private DateTime _Date;

            public DateTime Date
            {
                get { return this._Date; }
                set
                {
                    if (this._Date != value)
                    {
                        this._Date = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Date"));
                    }
                }
            }

            private IEnumerable<Essen> _Essen;

            public IEnumerable<Essen> Essen
            {
                get { return this._Essen; }
                set
                {
                    if (this._Essen != value)
                    {
                        this._Essen = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Essen"));
                    }
                }
            }

            public override string ToString()
            {
                return this.Date.ToShortDateString();
            }
        }

        public class Essen : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public Essen()
            {
                //avoid null checks
                this.PropertyChanged += (_, __) => { };
            }

            private string _Name;

            public string Name
            {
                get { return this._Name; }
                set
                {
                    if (this._Name != value)
                    {
                        this._Name = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                    }
                }
            }

            private string _Typ;

            public string Typ
            {
                get { return this._Typ; }
                set
                {
                    if (this._Typ != value)
                    {
                        this._Typ = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Typ"));
                    }
                }
            }

        }
    }
}

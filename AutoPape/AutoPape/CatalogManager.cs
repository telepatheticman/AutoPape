using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoPape
{
    class CatalogManager
    {
        public List<Catalog> catalogs;
        public List<string> boards;
        SettingsManager manager;
        Timer setPaper;
        Timer refresh;
        Timer archive;
        public CatalogManager(SettingsManager manager)
        {
            catalogs = new List<Catalog>();
            boards = new List<string>();
            setPaper = new Timer();
            refresh = new Timer();
            archive = new Timer();

            setPaper.Enabled = false;
            setPaper.Tick += new EventHandler(setPaperTick);

            refresh.Enabled = false;
            refresh.Tick += new EventHandler(refreshTick);

            archive.Tick += new EventHandler(archiveTick);
            archive.Interval = (int)Utility.msFromNowToTime("01:00:00");
            archive.Start();

            this.manager = manager;
            //setPaper.Interval = 10000;
        }

        public async void buildAllAsync()
        {
            await Task.Run(() => buildAll());
            long msToNextHour = Utility.msToNextHour();
            int toNextHalf = (int)msToNextHour + 1800000;
            if (msToNextHour <= 300000) msToNextHour += 3600000;
            setPaper.Interval = (int)msToNextHour;
            refresh.Interval = toNextHalf;
            setPaper.Start();
            //TODO: Uncomment when refresh is better
            //refresh.Start();
            setPaperNonTick();
        }
        private void buildAll()
        {
            foreach(var catalog in catalogs)
            {
                catalog.buildAsync();
            }
        }


        public void add(Catalog toAdd)
        {
            //TODO: Make this check better later
            if(!catalogs.Contains(toAdd))
            {
                catalogs.Add(toAdd);
            }

            if(!boards.Contains(toAdd.board))
            {
                boards.Add(toAdd.board);
            }
        }

        private void setPaperNonTick()
        {
            Random rand = new Random();
            int toUse = 0;
            do
            {
                toUse = rand.Next(0, catalogs.Count());
            } while (catalogs[toUse].threads.Count() > 0);
            catalogs[toUse].setWallpaperAsync();
        }

        private void setPaperTick(object sender, EventArgs e)
        {
            Random rand = new Random();
            int toUse = rand.Next(0, catalogs.Count());
            setPaper.Interval = 3600000;
            catalogs[toUse].setWallpaperAsync();
        }
        public void refreshTick(object sender, EventArgs e)
        {
            refresh.Interval = 3600000;
            foreach (var catalog in catalogs)
            {
                catalog.clear();
                catalog.buildAsync();
            }
        }

        public void archiveTick(object sender, EventArgs e)
        {
            archive.Interval = 86400000;
            foreach (var board in boards)
            {
                Catalog toSave = new Catalog(board, manager, catalogType.archive);
                toSave.buildAsync();
            }
        }
    }
}

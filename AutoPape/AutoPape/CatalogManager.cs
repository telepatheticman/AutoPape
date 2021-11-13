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
        Timer setPaper;
        Timer refreshAndArchive;
        public CatalogManager()
        {
            catalogs = new List<Catalog>();
            setPaper = new Timer();
            refreshAndArchive = new Timer();
        }

        public async void buildAllAsync()
        {
            await Task.Run(() => buildAll());
        }
        private void buildAll()
        {
            foreach(var catalog in catalogs)
            {
                catalog.buildAsync();
            }
        }

        public void refresh(bool shuffle)
        {
            foreach(var catalog in catalogs)
            {
                catalog.clear();
                catalog.buildAsync();
                if (shuffle) catalog.threads.Shuffle();
            }
        }

        public void add(Catalog toAdd)
        {
            //TODO: Make this check better later
            if(!catalogs.Contains(toAdd))
            {
                catalogs.Add(toAdd);
            }
        }

        private void setPaperTick()
        {

        }

        private void refreshTick()
        {

        }
    }
}

{
  "ShardEqualsPriority": 100.0,
  "DeviationLimitFromAverage": 0.5,
  "Collections": [
    {
      "Ns": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.Segments",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.Segments.MachineTranslations",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.Paragraphs",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.LingvoPro.TMUnits",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.LingvoPro.Concepts",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.Documents.Files.chunks",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.ContextImages.Files.chunks",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Priority": 1.0,
      "UnShardCompensation": true
    },
    {
      "Ns": "smartcat.Cat.CommentTopics",
      "Priority": 1.0,
      "UnShardCompensation": true
    }
  ],
  "Shards": [
    {
      "Name": "caravan",
      "UnShardedSize": 63346676630
    },
    {
      "Name": "frog",
      "UnShardedSize": 49470387477
    },
    {
      "Name": "zebra",
      "UnShardedSize": 5006864437
    },
    {
      "Name": "terra",
      "UnShardedSize": 0
    },
    {
      "Name": "koala",
      "UnShardedSize": 9767981081
    },
    {
      "Name": "zombie",
      "UnShardedSize": 18661174759
    },
    {
      "Name": "sheep",
      "UnShardedSize": 8118736
    },
    {
      "Name": "tomato",
      "UnShardedSize": 0
    }
  ],
  "Buckets": [
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "caravan",
      "Size": 4541701770,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "frog",
      "Size": 4000048079,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "zebra",
      "Size": 3060205352,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "terra",
      "Size": 5032719118,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "koala",
      "Size": 3885583163,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "zombie",
      "Size": 4497227117,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "sheep",
      "Size": 3191415689,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.GlossaryTermEntries",
      "Shard": "tomato",
      "Size": 3359147190,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "caravan",
      "Size": 5489225586,
      "Managed": false,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "frog",
      "Size": 114832924578,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "zebra",
      "Size": 119001097674,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "terra",
      "Size": 120507393495,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "koala",
      "Size": 116580220429,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "zombie",
      "Size": 114225154022,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "sheep",
      "Size": 122186570255,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments",
      "Shard": "tomato",
      "Size": 110846707744,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "caravan",
      "Size": 4207511927,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "frog",
      "Size": 4609125578,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "zebra",
      "Size": 4211281056,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "terra",
      "Size": 5401062208,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "koala",
      "Size": 4427941058,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "zombie",
      "Size": 4259865050,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "sheep",
      "Size": 4023289782,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Segments.MachineTranslations",
      "Shard": "tomato",
      "Size": 4335790645,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "caravan",
      "Size": 7365187375,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "frog",
      "Size": 7781219388,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "zebra",
      "Size": 7239935887,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "terra",
      "Size": 8018113771,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "koala",
      "Size": 8126220656,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "zombie",
      "Size": 6777448804,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "sheep",
      "Size": 7285245880,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Paragraphs",
      "Shard": "tomato",
      "Size": 6272026602,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "caravan",
      "Size": 200042508681,
      "Managed": true,
      "Min": 98104144265
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "frog",
      "Size": 138357573909,
      "Managed": true,
      "Min": 314640661
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "zebra",
      "Size": 160755590529,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "terra",
      "Size": 166384173317,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "koala",
      "Size": 157574850407,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "zombie",
      "Size": 154884528801,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "sheep",
      "Size": 164394018714,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.TMUnits",
      "Shard": "tomato",
      "Size": 157945357004,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "caravan",
      "Size": 6814756339,
      "Managed": true,
      "Min": 2989551091
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "frog",
      "Size": 2770439060,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "zebra",
      "Size": 4061517470,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "terra",
      "Size": 2777614149,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "koala",
      "Size": 2549849120,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "zombie",
      "Size": 2136799505,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "sheep",
      "Size": 2374531364,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.LingvoPro.Concepts",
      "Shard": "tomato",
      "Size": 2024970095,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "caravan",
      "Size": 1738949,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "frog",
      "Size": 1282779,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "zebra",
      "Size": 1349870,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "terra",
      "Size": 1794909,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "koala",
      "Size": 1388348,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "zombie",
      "Size": 1039003,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "sheep",
      "Size": 1380729,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.AsyncTasks.SendEMail.AttachedFiles.chunks",
      "Shard": "tomato",
      "Size": 1254691,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "caravan",
      "Size": 81744055697,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "frog",
      "Size": 80568506308,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "zebra",
      "Size": 84371073950,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "terra",
      "Size": 84334461170,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "koala",
      "Size": 90705292263,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "zombie",
      "Size": 86761102470,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "sheep",
      "Size": 87731689056,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Documents.Files.chunks",
      "Shard": "tomato",
      "Size": 86357177410,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "caravan",
      "Size": 317909416,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "frog",
      "Size": 346630781,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "zebra",
      "Size": 333074080,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "terra",
      "Size": 342162799,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "koala",
      "Size": 375034248,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "zombie",
      "Size": 327855749,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "sheep",
      "Size": 433844538,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.ContextImages.Files.chunks",
      "Shard": "tomato",
      "Size": 402936020,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "caravan",
      "Size": 3944563804,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "frog",
      "Size": 3446686027,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "zebra",
      "Size": 4275786430,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "terra",
      "Size": 2071494266,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "koala",
      "Size": 2610092278,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "zombie",
      "Size": 3534399866,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "sheep",
      "Size": 3330175054,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.NonTranslatableFiles.chunks",
      "Shard": "tomato",
      "Size": 3803199316,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "caravan",
      "Size": 175927914,
      "Managed": true,
      "Min": 108819050
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "frog",
      "Size": 165585562,
      "Managed": true,
      "Min": 98476698
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "zebra",
      "Size": 208785337,
      "Managed": true,
      "Min": 7458745
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "terra",
      "Size": 146604667,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "koala",
      "Size": 141825400,
      "Managed": true,
      "Min": 74716536
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "zombie",
      "Size": 136569735,
      "Managed": true,
      "Min": 2352007
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "sheep",
      "Size": 124832954,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Alignment.Documents.TmxFiles.chunks",
      "Shard": "tomato",
      "Size": 321484962,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "caravan",
      "Size": 264402069,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "frog",
      "Size": 276360939,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "zebra",
      "Size": 238442700,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "terra",
      "Size": 251012022,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "koala",
      "Size": 266221707,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "zombie",
      "Size": 242386086,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "sheep",
      "Size": 199854582,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.PreTranslate.ProcessedRepetitionsCache",
      "Shard": "tomato",
      "Size": 233435394,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "caravan",
      "Size": 350889,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "frog",
      "Size": 5694762,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "zebra",
      "Size": 494760,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "terra",
      "Size": 0,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "koala",
      "Size": 4277628,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "zombie",
      "Size": 731073,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "sheep",
      "Size": 562743,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.UpdateSegmentsRepetitionState.ProcessedSegmentsCache",
      "Shard": "tomato",
      "Size": 0,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "caravan",
      "Size": 366854496,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "frog",
      "Size": 411683286,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "zebra",
      "Size": 507755913,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "terra",
      "Size": 618139566,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "koala",
      "Size": 429298509,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "zombie",
      "Size": 431556363,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "sheep",
      "Size": 490331991,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat_nobackup.AsyncTasks.ProjectStatistics.ProcessedRepetitionsCache",
      "Shard": "tomato",
      "Size": 411687750,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "caravan",
      "Size": 8960957,
      "Managed": true,
      "Min": 8960957
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "frog",
      "Size": 8351798,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "zebra",
      "Size": 8466761,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "terra",
      "Size": 7759895,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "koala",
      "Size": 8374316,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "zombie",
      "Size": 7899798,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "sheep",
      "Size": 9272515,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.Projects.CostDetailsFiles.chunks",
      "Shard": "tomato",
      "Size": 0,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "caravan",
      "Size": 1148434942,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "frog",
      "Size": 1119763164,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "zebra",
      "Size": 1139179269,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "terra",
      "Size": 1119118019,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "koala",
      "Size": 1108363437,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "zombie",
      "Size": 1130378115,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "sheep",
      "Size": 1105756775,
      "Managed": true,
      "Min": 0
    },
    {
      "Collection": "smartcat.Cat.CommentTopics",
      "Shard": "tomato",
      "Size": 1104552901,
      "Managed": true,
      "Min": 0
    }
  ]
}
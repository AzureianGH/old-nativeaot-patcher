typedef unsigned char uint8_t;

extern uint8_t pcieids_start[];
extern uint8_t pcieids_end[];

struct pcieids_section {
    uint8_t* start;
    unsigned long long size;
};

static struct pcieids_section pcieids;

struct pcieids_section* getPcieIds()
{
    pcieids.start = pcieids_start;
    pcieids.size = (unsigned long long)(pcieids_end - pcieids_start);
    return &pcieids;
}
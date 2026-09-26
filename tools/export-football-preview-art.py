#!/usr/bin/python3
"""Export project-authored goal-facing SVG layers from the approved HTML preview.
Requires system PyGObject, librsvg and pycairo. Run with /usr/bin/python3.
"""
from pathlib import Path
from copy import deepcopy
import re
import xml.etree.ElementTree as ET
import gi
import cairo
gi.require_version("Rsvg", "2.0")
from gi.repository import Rsvg
ROOT = Path(__file__).resolve().parents[1]
html = (ROOT / "docs/proposals/football-goal-view.html").read_text()
svg = ET.fromstring(re.search(r"<svg .*?</svg>", html, re.S).group())
defs = svg.find("defs")
children = list(svg)
keeper = next(n for n in children if n.get("id") == "keeper")
player = next(n for n in children if n.get("id") == "striker")
ball = next(n for n in children if n.get("id") == "ball")
shadow = next(n for n in children if n.get("id") == "ballShadow")
marker = next(n for n in children if n.tag == "ellipse" and n.get("cy") == "488")
start, end = children.index(marker)+1, children.index(keeper)
art = ROOT / "Assets/_Project/Art/Football/GoalView"
art.mkdir(exist_ok=True)

def export(name, nodes, box, width, height, local=False):
    doc = ET.Element("svg", {"xmlns":"http://www.w3.org/2000/svg", "viewBox":box, "width":str(width),"height":str(height)})
    doc.append(deepcopy(defs))
    for source in nodes:
        node=deepcopy(source)
        if local: node.attrib.pop("transform", None)
        node.attrib.pop("opacity", None) if name=="crosshair" else None
        doc.append(node)
    data=ET.tostring(doc)
    (art / (name+".svg")).write_bytes(data)
    handle=Rsvg.Handle.new_from_data(data)
    surface=cairo.ImageSurface(cairo.FORMAT_ARGB32,width*2,height*2)
    ctx=cairo.Context(surface);ctx.scale(2,2)
    rect=Rsvg.Rectangle();rect.x=0;rect.y=0;rect.width=width;rect.height=height
    handle.render_document(ctx,rect)
    surface.write_to_png(str(art / (name+".png")))

export("field",children[1:start],"0 0 1200 675",1200,675)
goal_nodes=children[start:end]
export("goal",[n for n in goal_nodes if n.get("fill") != "url(#net)"],"290 145 620 170",620,170)
export("net",[n for n in goal_nodes if n.get("fill") == "url(#net)"],"290 145 620 170",620,170)
export("keeper",[keeper],"-70 -103 140 130",140,130,True)
export("player",[player],"-65 -115 140 230",140,230,True)
export("ball",[ball],"-18 -18 36 36",36,36,True)
export("shadow",[shadow],"576 494 48 16",48,16)
export("crosshair",[next(n for n in children if n.get("id")=="target")],"-30 -30 60 60",60,60,True)
export("panel",[ET.fromstring('<rect x="0" y="0" width="32" height="32" rx="6" fill="white"/>')],"0 0 32 32",32,32)
export("fill",[ET.fromstring('<rect width="8" height="8" fill="white"/>')],"0 0 8 8",8,8)
export("knob",[ET.fromstring('<circle cx="16" cy="16" r="15" fill="white"/>')],"0 0 32 32",32,32)
print("Exported 11 original SVG/PNG Football layers to", art)

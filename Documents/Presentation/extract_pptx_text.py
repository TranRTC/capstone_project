import re
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
    "p": "http://schemas.openxmlformats.org/presentationml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
}

def extract_text_from_slide_xml(xml_bytes):
    root = ET.fromstring(xml_bytes)
    a_ns = NS["a"]
    texts = []
    for t in root.iter("{%s}t" % a_ns):
        if t.text:
            texts.append(t.text)
    return texts

def get_slide_order(z):
    try:
        pres = z.read("ppt/presentation.xml")
        root = ET.fromstring(pres)
        p_ns = NS["p"]
        r_ns = NS["r"]
        slide_ids = []
        for sld_id in root.iter("{%s}sldId" % p_ns):
            rid = sld_id.get("{%s}id" % r_ns)
            if rid:
                slide_ids.append(rid)
        rels = z.read("ppt/_rels/presentation.xml.rels")
        rel_root = ET.fromstring(rels)
        rid_to_target = {}
        for rel in rel_root:
            if rel.tag.endswith("Relationship"):
                rid_to_target[rel.get("Id") or ""] = rel.get("Target") or ""
        ordered_paths = []
        for rid in slide_ids:
            target = rid_to_target.get(rid, "")
            if target.startswith("../"):
                target = "ppt/" + target[3:]
            elif target and not target.startswith("ppt/"):
                target = "ppt/" + target
            ordered_paths.append(target.replace("\\", "/"))
        return ordered_paths
    except Exception:
        slides = sorted(
            [n for n in z.namelist() if re.match(r"ppt/slides/slide\d+\.xml", n)],
            key=lambda x: int(re.search(r"slide(\d+)", x).group(1)),
        )
        return slides

def extract_pptx(path, full=True):
    path = Path(path)
    if not path.exists():
        return None
    out = []
    with zipfile.ZipFile(path, "r") as z:
        slide_paths = get_slide_order(z)
        for i, sp in enumerate(slide_paths, 1):
            sp_norm = sp.replace("\\", "/")
            if sp_norm not in z.namelist():
                alt = sp_norm.split("/")[-1]
                candidates = [n for n in z.namelist() if n.endswith(alt)]
                if not candidates:
                    out.append("=== SLIDE %d ===\n(missing: %s)\n" % (i, sp_norm))
                    continue
                sp_norm = candidates[0]
            xml = z.read(sp_norm)
            texts = [t.strip() for t in extract_text_from_slide_xml(xml) if t and t.strip()]
            if full:
                out.append("=== SLIDE %d ===" % i)
                out.append("\n".join(texts) if texts else "(no text)")
                out.append("")
            else:
                brief = " | ".join(texts[:8])
                if len(texts) > 8:
                    brief += " ... (+%d more)" % (len(texts) - 8)
                label = brief if texts else "(no text)"
                out.append("Slide %d: %s" % (i, label))
    return "\n".join(out)

base = Path(r"C:\Spring 2026\capstone_project\Documents\Presentation")
main_path = base / "Capstone Project Presentation.pptx"
diag_path = base / "Capstone_System_Architecture_Diagrams.pptx"
out_path = base / "_extracted_pptx_text.txt"

parts = []
parts.append("=" * 80)
parts.append("CAPSTONE PROJECT PRESENTATION")
parts.append("=" * 80)
main_text = extract_pptx(main_path, full=True)
parts.append(main_text or "ERROR")
parts.append("=" * 80)
parts.append("CAPSTONE SYSTEM ARCHITECTURE DIAGRAMS")
parts.append("=" * 80)
if diag_path.exists():
    parts.append(extract_pptx(diag_path, full=False) or "ERROR")
else:
    parts.append("FILE NOT FOUND")

out_path.write_text("\n".join(parts), encoding="utf-8")
print("Wrote:", out_path)
print("Slides:", main_text.count("=== SLIDE") if main_text else 0)
print("Diagrams exists:", diag_path.exists())

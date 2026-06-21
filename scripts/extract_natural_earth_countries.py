import json
import sqlite3
import struct
from pathlib import Path


SOURCE = Path("data/packages/natural_earth_vector.gpkg")
OUTPUT = Path("data/natural_earth_countries.geojson")
TABLE = "ne_10m_admin_0_countries"


def read_uint32(data, offset, endian):
    return struct.unpack_from(endian + "I", data, offset)[0], offset + 4


def read_double(data, offset, endian):
    return struct.unpack_from(endian + "d", data, offset)[0], offset + 8


def skip_gpkg_header(data):
    if data[:2] != b"GP":
        raise ValueError("Invalid GeoPackage geometry header")

    flags = data[3]
    envelope_indicator = (flags >> 1) & 0b111
    offset = 8
    envelope_sizes = {
        0: 0,
        1: 32,
        2: 48,
        3: 48,
        4: 64,
    }
    return offset + envelope_sizes.get(envelope_indicator, 0)


def parse_wkb(data, offset=0):
    endian = "<" if data[offset] == 1 else ">"
    offset += 1
    geometry_type, offset = read_uint32(data, offset, endian)
    geometry_type = geometry_type & 0xFFFF

    if geometry_type == 3:
        return parse_polygon(data, offset, endian)

    if geometry_type == 6:
        polygon_count, offset = read_uint32(data, offset, endian)
        polygons = []
        for _ in range(polygon_count):
            polygon, offset = parse_wkb(data, offset)
            polygons.extend(polygon)
        return polygons, offset

    raise ValueError(f"Unsupported WKB geometry type: {geometry_type}")


def parse_polygon(data, offset, endian):
    ring_count, offset = read_uint32(data, offset, endian)
    rings = []
    for _ in range(ring_count):
        point_count, offset = read_uint32(data, offset, endian)
        ring = []
        for _ in range(point_count):
            longitude, offset = read_double(data, offset, endian)
            latitude, offset = read_double(data, offset, endian)
            ring.append([round(longitude, 7), round(latitude, 7)])
        rings.append(ring)

    return [rings], offset


def parse_geometry(blob):
    wkb_offset = skip_gpkg_header(blob)
    polygons, _ = parse_wkb(blob, wkb_offset)
    return {
        "type": "MultiPolygon",
        "coordinates": polygons,
    }


def normalize_code(iso_a2, adm0_a3):
    if iso_a2 and iso_a2 != "-99" and len(iso_a2) == 2:
        return iso_a2

    return adm0_a3


def main():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    connection = sqlite3.connect(SOURCE)
    cursor = connection.cursor()
    rows = cursor.execute(
        f"""
        select ISO_A2, ADM0_A3, NAME_EN, NAME, geom
        from {TABLE}
        where geom is not null
        """
    )

    features = []
    for iso_a2, adm0_a3, name_en, name, geom in rows:
        code = normalize_code(iso_a2, adm0_a3)
        if not code or code == "-99":
            continue

        features.append(
            {
                "type": "Feature",
                "properties": {
                    "code": code,
                    "name": name_en or name or code,
                },
                "geometry": parse_geometry(geom),
            }
        )

    connection.close()
    OUTPUT.write_text(
        json.dumps(
            {
                "type": "FeatureCollection",
                "name": "natural_earth_countries",
                "features": features,
            },
            ensure_ascii=False,
            separators=(",", ":"),
        ),
        encoding="utf-8",
    )
    print(f"Wrote {len(features)} features to {OUTPUT}")


if __name__ == "__main__":
    main()

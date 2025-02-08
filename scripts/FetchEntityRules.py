#!/usr/bin/env python3

# Requires `pip install pandas`!

import pandas
import json

SHEET_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vRXrDu4gWcPOXSWMhwlffAwYxHhj-c0jsMIn6MAuaPZY26tyi2Or7WtKHnRE24stkSE_nI6FX6JhIn1/pub?gid=0&single=true&output=csv"

data = pandas.read_csv(SHEET_URL, index_col=False)
output = {}

# Must correspond to EntityRule.AllowLevel
Levels = { "allow": 3, "warn": 2, "deny": 1 }

for line, row in data.iterrows():
    classname = row.get("ClassName")
    level = row.get("AllowLevel")
    comment = row.get("Comment",)
    if not classname:
        raise f"empty classname on line {line}"

    if classname in output:
        raise f"duplicate classname {classname} on line {line}"

    if not level:
        raise f"empty level on line {line}"

    if level not in Levels:
        raise f"unknown level {level} on line {line}"

    ent = {"Level": Levels[level]}

    if comment and not pandas.isna(comment):
        ent["Comment"] = comment

    output[classname] = ent

with open('../resources/entityrules_momentum.json', 'w') as outfile:
    json.dump(output, outfile, indent=4)
    print("Wrote entity rules")

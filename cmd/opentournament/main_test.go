package main

import (
	"os/exec"
	"slices"
	"strings"
	"testing"
)

// The container image has no time zone database, and a Recurring Tournament
// needs one to read its schedule in its time zone. A host that has one can't
// show it missing, so the test checks the build instead.
func TestTheBinaryEmbedsTheTimeZoneDatabase(t *testing.T) {
	out, err := exec.Command("go", "list", "-deps", ".").Output()
	if err != nil {
		t.Fatalf("go list -deps: %v", err)
	}
	if deps := strings.Fields(string(out)); !slices.Contains(deps, "time/tzdata") {
		t.Errorf("go list -deps lists no time/tzdata, want the binary to embed the time zone database")
	}
}

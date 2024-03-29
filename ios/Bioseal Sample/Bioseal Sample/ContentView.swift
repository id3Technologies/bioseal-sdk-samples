//
//  ContentView.swift
//  Bioseal Sample
//
//  Created by Antoine TOMBU on 27/06/2023.
//

import SwiftUI

struct ContentView: View {
    
    init() {
        Sample().run()
    }
    
    var body: some View {
        VStack {
            Image(systemName: "globe")
                .imageScale(.large)
                .foregroundColor(.accentColor)
            Text("Hello, world!")
        }
        .padding()
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}
